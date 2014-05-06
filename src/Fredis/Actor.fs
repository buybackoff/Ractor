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
// TODO resultId should work like the channel in MPB.PostAndReply
// each actor must know for whom it does job. of "for" property is empty, then it does
// job for a caller that is not an actor. If "for" is set, then the result must be 
// handed over to continuation actor's (pipeline/inbox?) atomically inside redis 
// so if the calling actor's incarnation dies another one will pick up
// Make an inteface with Actor being private? 
type Actor<'Task, 'TResult> internal (redis : Redis, id : string, computation : 'Task -> Async<'TResult>, lowPriority : bool) = 
    // linking only works on children with computations returning unit
    // let children = Dictionary<string, Actor<'TResult, unit>>()
    let mutable started = false
    let mutable cts = Unchecked.defaultof<CancellationTokenSource>
    let awaitMessageHandle = new AutoResetEvent(false)
    let expectedResultsHandles = Dictionary<string, AutoResetEvent>()
    let prefix = id + ":Mailbox"
    // list of incoming messages
    let inboxKey = prefix + ":inbox" // TODO message is a tuple of resultId * callerId * payload
    // hash of messages being processed
    let pipelineKey = prefix + ":pipeline"
    // hash of results not yet claimed by callers
    let resultsKey = prefix + ":results" // TODO results must have "for" property
    let channelKey = prefix + ":channel"
    let errorsKey = prefix + ":errors"
    [<DefaultValue>] val mutable internal errorHandler : Actor<ExceptionInfo<'Task>, unit> ref
    [<DefaultValue>] val mutable internal semaphor : SemaphoreSlim
    // TODO
    let lowPriority = false
    // this could be set from outside and block execution of low-priority tasks
    // could be used to guarantee execution of important task without waiting for autoscale
    // e.g. simple rule if CPU% > 80% for a minute then suspend low-priority actors
    // and resume when CPU% falls below 50%. If then we set autoscale rule at 65% 5-min
    // the autoscale group will grow only when high-priority tasks consume > 65% for several minutes
    [<DefaultValue>] val mutable internal lowPriorityGate : ManualResetEventSlim
    [<DefaultValue>] val mutable internal counter : int ref
    new() = Actor(null, null, Unchecked.defaultof<'Task -> Async<'TResult>>, false)
    member private this.Id = id
    member private this.Redis = redis
    member private this.Computation = computation
    member this.QueueLength = int (redis.LLen(inboxKey))
    
    // public error handling should be C# friendly
    member this.ErrorHandler 
        with internal get () = !this.errorHandler
        and internal set (eh) = this.errorHandler := eh
    
    member this.Start() : unit = 
        if not started then 
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
                    let! message = redis.EvalAsync<'T * string>(lua, 
                                                                [| redis.KeyNameSpace + ":" + inboxKey
                                                                   redis.KeyNameSpace + ":" + pipelineKey
                                                                   pipelineId |])
                                   |> Async.AwaitTask
                    if Object.Equals(message, null) then 
                        let! signal = Async.AwaitWaitHandle(awaitMessageHandle, timeout)
                        if signal then return! await timeout
                        else return raise (TimeoutException("Receive timed out"))
                    else return message, pipelineId
                }
            
            let waitLowPriorityGateIsOpen = 
                async { 
                    if lowPriority && this.lowPriorityGate <> Unchecked.defaultof<ManualResetEventSlim> then 
                        do! Async.AwaitWaitHandle(this.lowPriorityGate.WaitHandle) |> Async.Ignore
                }
            
            redis.Subscribe(channelKey, 
                            Action<string, string>(fun channel message -> 
                                match message with
                                | "" -> awaitMessageHandle.Set() |> ignore
                                | resultId -> 
                                    if expectedResultsHandles.ContainsKey(resultId) then 
                                        expectedResultsHandles.[resultId].Set() |> ignore
                                    else failwith "wrong result id"))
            cts <- new CancellationTokenSource()
            async { 
                while (not cts.Token.IsCancellationRequested) do
                    do! this.semaphor.WaitAsync(cts.Token)
                        |> Async.AwaitIAsyncResult
                        |> Async.Ignore
                    do! waitLowPriorityGateIsOpen
                    try 
                        let! msg = await Timeout.Infinite
                        let payload = (fst (fst msg))
                        let messageId = (snd (fst msg))
                        let pipelineId = snd msg
                        async { 
                            try 
                                let! result = computation payload
                                //children |> Seq.iter (fun a -> a.Value.Post(result)) // make one atomic call to redis here
                                if messageId <> "" then 
                                    redis.HSet(resultsKey, messageId, result, When.Always, true) |> ignore
                                    redis.Publish<string>(channelKey, messageId, true) |> ignore
                                redis.HDel(pipelineKey, pipelineId, true) |> ignore
                            with e -> 
                                let ei = ExceptionInfo(id, payload, e)
                                redis.LPush<ExceptionInfo<'Task>>(errorsKey, ei, When.Always, true) |> ignore
                                if this.errorHandler.Value <> Unchecked.defaultof<Actor<ExceptionInfo<'Task>, _>> then 
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
    
    member this.Post<'Tin>(message : 'Task) : unit = 
        // TODO? local execution if started? similar to PostAndReply.
        // that will be good for chained actors - will save one trip to Redis and will have data to process right now, while pipeline is fire-and-forget write
        // must get ACK that message saved before returning
        // use synchronous method
        // TODO error handling
        redis.LPush<'Task * string>(inboxKey, (message, ""), When.Always, false) |> ignore
        awaitMessageHandle.Set() |> ignore
        redis.Publish<string>(channelKey, "", true) |> ignore
    
    abstract PostAndReply : 'Task -> Async<'TResult>
    override this.PostAndReply(message : 'Task) : Async<'TResult> = 
        this.PostAndReply(message, Timeout.Infinite, "", null, "")

    abstract PostAndReply : 'Task * int -> Async<'TResult>
    override this.PostAndReply(message : 'Task, millisecondsTimeout) : Async<'TResult> = 
        this.PostAndReply(message, millisecondsTimeout, "", null, "")
    
    // TODO make internal method, resultId and continuation to be used with ContinueWith
    abstract PostAndReply :'Task * int * string *  Redis * string -> Async<'TResult>
    override this.PostAndReply(message : 'Task, millisecondsTimeout, callerId : string, callerRedis : Redis, 
                                      resultId : string) : Async<'TResult> = 
        let callId = 
            if String.IsNullOrWhiteSpace(callerId) then ""
            else callerId
        
        let resId = 
            if String.IsNullOrWhiteSpace(resultId) then Guid.NewGuid().ToString("N")
            else resultId
        
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
        | _ -> // false
               
            expectedResultsHandles.Add(resId, new AutoResetEvent(false)) |> ignore
            redis.LPush<'Task * string>(inboxKey, (message, resId)) |> ignore
            // no resultId here because we notify recievers that in turn will notify callers about results
            redis.Publish<string>(channelKey, "", true) |> ignore
            let rec awaitResult count = 
                async { 
                    let waitHandle = expectedResultsHandles.[resId]
                    
                    // expect that notification will come for expected result id
                    // do not trust PubSub yet, in Redis docs they say messages could be lost
                    // divide given timeout by 3  to check results set before the given timeout
                    let timeout = 
                        if millisecondsTimeout = Timeout.Infinite then millisecondsTimeout
                        else millisecondsTimeout / 3
                    //Debug.Print("waiting for signal")
                    let! signal = Async.AwaitWaitHandle(waitHandle, timeout)
                    if signal then 
                        //Debug.Print("received signal")
                        let! result = redis.HGetAsync<'TResult>(resultsKey, resId) |> Async.AwaitTask
                        if Object.Equals(result, null) then 
                            Debug.Fail("Expected to get the right result after the signal")
                        expectedResultsHandles.[resId].Dispose()
                        // TODO if there is a continuation, pass to it directly
                        redis.HDel(resultsKey, resId, true) |> ignore
                        return result
                    else // timeout
                         
                        //Debug.Print("didn't received signal")
                        if count > 3 then Debug.Fail("Cannot receive result for PostAndReply")
                        return! awaitResult (count + 1)
                }
            async { let! t = Async.StartChild(awaitResult 1, millisecondsTimeout)
                    return! t }
    
    member this.PostAndReplyAsync(message : 'Task) : Task<'TResult> = this.PostAndReplyAsync(message, Timeout.Infinite)
    
    // C# naming style and return type
    member this.PostAndReplyAsync(message : 'Task, millisecondsTimeout) : Task<'TResult> = 
        let res : Async<'TResult> = this.PostAndReply(message, millisecondsTimeout)
        res |> Async.StartAsTask
    
    // TODO
    member this.ContinueWith(continuation : Actor<'TResult, 'TResult2>) : Actor<'Task, 'TResult2> = 
        { // set resultId 
          // set callerId=continuation.Id
          // PaR on this and wait
          // PaR on continuation
          new Actor<'Task, 'TResult2>() with
              member __.PostAndReply(message : 'Task, millisecondsTimeout, callerId : string, callerRedis : Redis, 
                                      resultId : string) : Async<'TResult2> =
              //member __.PostAndReply(message : 'Task, millisecondsTimeout) : Async<'TResult2> = 
                  let resultId = Guid.NewGuid().ToString("N")
                  let callerId = continuation.Id
                  let callerRedis = continuation.Redis
                  async { 
                      // reliable TResult exchange happens inside internal PostAndReply
                      let! computation = async { let! result = this.PostAndReply
                                                                   (message, millisecondsTimeout, callerId, callerRedis, 
                                                                    resultId)
                                                 return continuation.PostAndReply(result) }
                      let! child = Async.StartChild(computation, millisecondsTimeout)
                      return! child } }
    
    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            awaitMessageHandle.Dispose()
            cts.Dispose()
