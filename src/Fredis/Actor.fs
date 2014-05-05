namespace Fredis

open System
open System.Linq
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open Fredis

// TODO exception handling: probably add Optional/SuccessValue to be returned from Async and bool for Post?
// definetly a caller must get ACK to know what to do with a message
// another option is to provide optional error handler to actors with some default (log/notify)
// or do both: methods with TryPost and methods with handlers
// TODO A system actor that periodically checks pipelines and errors of all actors
// and is subscribed to errors channel as well

// TODO Wrong logic with children? Check
// now we have Finaliazers<Tout,unit> not Continuations
// since we check that an actor is started locally and run computation locally,
// it is better to have a method this.ContinueWith(Actor<'a,'b>):'b
// still need to store results from first with ACK in pipeline of the second before
// it could start

type internal ExceptionInfo<'T> = 
    | ExceptionInfo of string * 'T * Exception


type Actor<'Tin, 'Tout> internal (redis : Redis, id : string, computation : 'Tin -> Async<'Tout>, lowPriority:bool) as this = 
    // linking only works on children with computations returning unit
    let children = Dictionary<string, Actor<'Tout, unit>>()
    let mutable started = false
    let mutable cts = Unchecked.defaultof<CancellationTokenSource>
    let awaitMessageHandle = new AutoResetEvent(false)

    //let awaitResultHandle = new AutoResetEvent(false)
    let expectedResultsHandles = Dictionary<string, AutoResetEvent>()

    let prefix = id + ":Mailbox"
    let inboxKey = prefix + ":inbox"
    let pipelineKey = prefix + ":pipeline"
    let resultsKey = prefix + ":results"
    let channelKey = prefix + ":channel"
    let errorsKey = prefix + ":errors"

    [<DefaultValue>] val mutable internal errorHandler : Actor<ExceptionInfo<'Tin>, unit> ref

    [<DefaultValue>] val mutable internal semaphor : SemaphoreSlim

    // TODO
    let lowPriority = false
    // this could be set from outside and block execution of low-priority tasks
    // could be used to guarantee execution of important task without waiting for autoscale
    // e.g. simple rule if CPU% > 80% for a minute then suspend low-priority actors
    // and resume when CPU% falls below 50%. If then we set autoscale rule at 65% 5-min
    // the autoscale group will grow only when high-priority tasks consume > 65% for several minutes
    [<DefaultValue>] val mutable internal lowPriorityGate : ManualResetEventSlim
    let waitLowPriorityGateIsOpen = 
        async {
            if lowPriority && this.lowPriorityGate <> Unchecked.defaultof<ManualResetEventSlim> then
                do! Async.AwaitWaitHandle(this.lowPriorityGate.WaitHandle) |> Async.Ignore
        }

    [<DefaultValue>] val mutable internal counter : int ref

    let rec await timeout = 
        async { 
            //let! message = !!redis.RPopAsync("")
            // atomically move to safe place while processing
            let lua = @"
local result = redis.call('RPOP', KEYS[1])
if result ~= nil then
    redis.call('HSET', KEYS[2], KEYS[3], result)
end
return result"
            // TODO add ZSet with timestamp as rank to monitor the pipeline state
            let pipelineId = Guid.NewGuid().ToString("N")
            
            let! message = 
                redis.EvalAsync<'T * string>(lua, 
                                        [| redis.KeyNameSpace + ":" + inboxKey
                                           redis.KeyNameSpace + ":" + pipelineKey
                                           pipelineId |]) |> Async.AwaitTask
            if Object.Equals(message, null) then 
                let! signal = Async.AwaitWaitHandle(awaitMessageHandle, timeout)
                if signal then return! await timeout
                else return raise (TimeoutException("Receive timed out"))
            else return message, pipelineId
        }
    
    member this.Id = id
    member this.Children = children.Keys.ToArray()
    member this.QueueLength = int (redis.LLen(inboxKey))
    
    // public error handling should be C# friendly
    member internal this.ErrorHandler 
        with get () = !this.errorHandler
        and set (eh) = this.errorHandler := eh
    
    member private this.Computation = computation
    
    member this.Start() : unit = 
        redis.Subscribe(channelKey, 
            Action<string, string>(fun channel message -> 
                match message with
                | "" -> awaitMessageHandle.Set() |> ignore
                | x -> 
                    if expectedResultsHandles.ContainsKey(x) then
                        expectedResultsHandles.[x].Set() |> ignore))

        cts <- new CancellationTokenSource()
        async { 
            while (not cts.Token.IsCancellationRequested) do
                do! this.semaphor.WaitAsync(cts.Token) |> Async.AwaitIAsyncResult |> Async.Ignore
                do! waitLowPriorityGateIsOpen
                try 
                    let! msg = await Timeout.Infinite
                    let payload = (fst (fst msg))
                    let messageId = (snd (fst msg))
                    let pipelineId = snd msg
                    async { 
                        try 
                            let! result = computation payload
                            children |> Seq.iter (fun a -> a.Value.Post(result)) // make one atomic call to redis here
                            if messageId <> "" then 
                                redis.HSet(resultsKey, messageId, result, When.Always, true) |> ignore
                                redis.Publish<string>(channelKey, messageId, true) |> ignore
                            redis.HDel(pipelineKey, pipelineId, true) |> ignore
                        with e -> 
                            let ei = ExceptionInfo(id, payload, e)
                            redis.LPush<ExceptionInfo<'Tin>>(errorsKey, ei, When.Always, true) |> ignore
                            if this.errorHandler.Value <> Unchecked.defaultof<Actor<ExceptionInfo<'Tin>, _>> then 
                                this.errorHandler.Value.Post(ei)
                    }
                    |> Async.Start
                finally
                    this.semaphor.Release() |> ignore
        }
        |> Async.Start
        started <- true
    
    member this.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
    

    member this.Post<'Tin>(message : 'Tin) : unit = 
        // TODO? local execution if started? similar to PostAndReply.
        // that will be good for chained actors - will save one trip to Redis and will have data to process right now, while pipeline is fire-and-forget write
        
        // must get ACK that message saved before returning
        // use synchronous method
        // TODO error handling
        redis.LPush<'Tin * string>(inboxKey, (message, ""), When.Always, false) |> ignore
        awaitMessageHandle.Set() |> ignore
        redis.Publish<string>(channelKey, "", true) |> ignore
    
    member this.PostAndReply(message : 'Tin) : Async<'Tout> = 
        this.PostAndReply(message, Timeout.Infinite)

    member this.PostAndReply(message : 'Tin, millisecondsTimeout) : Async<'Tout> = 
        match started with
//        | true -> 
//            let pipelineId = Guid.NewGuid().ToString("N")
//            // PostAndReply on started actor always executes
//            // TODO increment/decrement counters/semaphor
//            async { 
//                let! savedTask = 
//                    redis.HSetAsync<'Tin>(pipelineKey, pipelineId, message, When.Always, true) // save message
//                    |> Async.AwaitTask 
//                    |> Async.StartChild 
//                let! result = computation message
//                let! saved = savedTask // TODO error handling
//                children |> Seq.iter (fun a -> a.Value.Post(result))
//                redis.HDel(pipelineKey, pipelineId, true) |> ignore
//                return result
//            }
        | _ ->  // false
            let resultId = Guid.NewGuid().ToString("N")
            expectedResultsHandles.Add(resultId, new AutoResetEvent(false)) |> ignore
            
            // WRONG LOGIC HERE, it is not a single loop but multiple calls could be made to PaR at the same time
            // each request must have its own waitHandle
            // dict should store handles by ids
            // handle should timeout in reasonable time, probably with exp growth
            let rec awaitResult count = 
                async { 
                    do! redis.LPushAsync<'Tin * string>(inboxKey, (message, resultId)) 
                        |> Async.AwaitTask |> Async.Ignore // wait for comletion

                    // no resultId here because we notify recievers that in turn will notify callers about results
                    redis.Publish<string>(channelKey, "", true) |> ignore 
            
                    // expect that notification will come for expected result id
                    // do not trust PubSub yet, in Redis docs they say messages could be lost
                    // divide given timeout by 3  to check results set before the given timeout
                    let! signal = Async.AwaitWaitHandle(expectedResultsHandles.[resultId], millisecondsTimeout / 3) 
                    if signal then 
                        let! message = redis.HGetAsync<'Tout>(resultsKey, resultId) |> Async.AwaitTask
                        if Object.Equals(message, null) then Debug.Fail("Expected to get the right result after the signal")
                        expectedResultsHandles.[resultId].Dispose()
                        // TODO linked Actors should work for PaR as well
                        // run linked async and delete intermediate result only after
                        redis.HDel(resultsKey, resultId, true) |> ignore
                        return message
                    else  // timeout
                        if count > 10 then Debug.Fail("Cannot receive result for PostAndReply")
                        return! awaitResult (count + 1)
                }
            async {
                let! t = Async.StartChild(awaitResult 1, millisecondsTimeout)
                return! t
            }
            
    
    
    member this.PostAndReplyAsync(message : 'Tin) : Task<'Tout> = 
        this.PostAndReplyAsync(message, Timeout.Infinite)
    
    // C# naming style and return type
    member this.PostAndReplyAsync(message : 'Tin, millisecondsTimeout) : Task<'Tout> = 
        let res : Async<'Tout> = 
            this.PostAndReply(message, millisecondsTimeout)
        res |> Async.StartAsTask
    
    member this.Link(actor : Actor<'Tout, unit>) = 
        children.Add(actor.Id, actor)
        this
    
    member this.Link(actors : IEnumerable<Actor<'Tout, unit>>) = 
        Seq.iter (fun (a : Actor<'Tout, _>) -> children.Add(a.Id, a)) actors
        this
    
    member this.UnLink(actor : Actor<'Tout, unit>) : bool = children.Remove(actor.Id)
    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            awaitMessageHandle.Dispose()
            cts.Dispose()
