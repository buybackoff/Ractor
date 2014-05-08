namespace Fredis

open System
open System.Linq
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Runtime.Caching
open Fredis

// TODO A system actor that periodically checks pipelines and errors of all actors
// and is subscribed to errors channel as well
// TODO pipelineCleaner run every N seconds, get all fields, store and in N seconds 
// compare with new fields. If intersection is not empty then return messages to the queue
// same with Results (+notification and persistence of unclaimed results)
// Timeout should be a property of actor, not param of methods - it is part of 
// computation, not message - it is computationTimeout (retry after timeout)
// method call should return eventually
// then we could know how often to check for pipeline and unclaimed results

type Actor<'Task, 'TResult> internal (redisConnectionString : string, id : string, computation : 'Task * string -> Async<'TResult>, computationTimeout:int, lowPriority : bool) = 
    let redis = new Redis(redisConnectionString, "Fredis")
    
    // linking only works on children with computations returning unit
    // let children = Dictionary<string, Actor<'TResult, unit>>()
    let mutable started = false
    let mutable cts = new CancellationTokenSource()
    let messageWaiter = new AsyncAutoResetEvent()
    let resultWaiters = ConcurrentDictionary<string, AsyncManualResetEvent>()
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
    
    static let actors = Dictionary<string, obj>()
    static member internal ActorsRepo with get () = actors
    
    static let resultsCache = MemoryCache.Default
    let messageQueue = ConcurrentQueue<('Task * string * string[]) * string>()

    member internal this.Id = id
    member internal this.RedisConnectionString = redisConnectionString
    member internal this.Computation = computation
    member internal this.ComputationTimeout = computationTimeout
    member internal this.LowPriority = lowPriority
    member this.QueueLength = (int (redis.LLen(inboxKey))) + messageQueue.Count
    
    // public error handling should be C# friendly
    member this.ErrorHandler 
        with internal get () = !this.errorHandler
        and internal set (eh) = this.errorHandler := eh
    
    member this.Start() : unit = 
        if not started then 
            let rec awaitMessage () = 
                async { 
                    //Debug.Print("Awaiting message")
                    // atomically move to safe place while processing
                    let lua = @"
                    local result = redis.call('RPOP', KEYS[1])
                    if result ~= nil then
                        redis.call('HSET', KEYS[2], KEYS[3], result)
                    end
                    return result"
                    let pipelineId = Guid.NewGuid().ToString("N")
                    let hasLocal, localMessage = messageQueue.TryDequeue()
                    if hasLocal then
                        //Debug.Print("Took local message")
                        return localMessage
                    else
                        let! message = redis.EvalAsync<'Task * string * string[]>(lua, 
                                                                    [| redis.KeyNameSpace + ":" + inboxKey
                                                                       redis.KeyNameSpace + ":" + pipelineKey
                                                                       pipelineId |])
                                       |> Async.AwaitTask
                        if Object.Equals(message, Unchecked.defaultof<'Task * string>) then
                            let! signal = messageWaiter.WaitAsync(1000) |> Async.AwaitTask // TODO timeout, if PubSub dropped notification, recheck the queue, but not very often
                            if not signal then Debug.Print("Timeout in awaitMessage") 
                            return! awaitMessage()
                        else
                            //Debug.Print("Took Redis message") 
                            return message, pipelineId
                }
            
            let waitGateIsOpen = 
                async { 
                    if lowPriority && this.lowPriorityGate <> Unchecked.defaultof<ManualResetEventSlim> then 
                        do! Async.AwaitWaitHandle(this.lowPriorityGate.WaitHandle) |> Async.Ignore
                }
            
            redis.Subscribe(channelKey, 
                            Action<string, string>(fun channel messageNotification -> 
                                match messageNotification with
                                | "" -> messageWaiter.Set() |> ignore
                                | resultId -> 
                                    if resultWaiters.ContainsKey(resultId) then
                                        //Debug.Print("Setting result handle: " + resultId)
                                        resultWaiters.[resultId].Set() |> ignore
                                    else failwith "wrong result id"))
            cts <- new CancellationTokenSource()
            let loop = 
                async { 
                    while (not cts.Token.IsCancellationRequested) do
                        // TODO instead of semaphor use dedicated worker threads = number of processors
                        
                        //Debug.Print("Before semaphor: " + this.semaphor.CurrentCount.ToString())
//                        do! this.semaphor.WaitAsync(cts.Token)
//                            |> Async.AwaitIAsyncResult
//                            |> Async.Ignore
                        //Debug.Print("Before gate")
                        Interlocked.Increment(this.counter) |> ignore
                        //do! waitGateIsOpen
                        let! (message, resultId, callerIds), pipelineId = awaitMessage()
                        //Debug.Print("Received message: " + resultId)
                        async {
                            try
                                try 
                                    let! result = computation(message, resultId)
                                    //Debug.Print("Completed computation for: " + resultId)
                                    if resultId <> "" then // then someone is waiting for the result
                                        resultsCache.Add(resultId, result, DateTimeOffset.Now.AddSeconds(10.0)) |> ignore
                                        // TODO trace cache hits and test performance with and without it

                                        if resultWaiters.ContainsKey(resultId) then
                                            resultWaiters.[resultId].Set() |> ignore
                                        else
                                            do! redis.HSetAsync(resultsKey, resultId, result, When.Always, false) 
                                                |> Async.AwaitTask |> Async.Ignore
                                            redis.Publish<string>(channelKey, resultId, true) |> ignore 
                                    redis.HDel(pipelineKey, pipelineId, true) |> ignore 
                                with e -> 
                                    // TODO rework this
                                    let ei = ExceptionInfo(id, message, e)
                                    redis.LPush<ExceptionInfo<'Task>>(errorsKey, ei, When.Always, true) |> ignore
                                    //if this.errorHandler <> Unchecked.defaultof<Actor<ExceptionInfo<'Task>, _> ref> then 
                                    //    this.errorHandler.Value.Post(ei)
                            finally
                                Interlocked.Decrement(this.counter) |> ignore
                                //this.semaphor.Release() |> ignore
                                ()
                        }
                        |> Async.Start // do as many task as global limits (semaphor, priority gate) alow 
                }
            Async.Start(loop, cts.Token)
            started <- true
    
    member this.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
    
    // then all other methods are just combinations of those
    member this.Post<'Tin>(message : 'Task) : unit = 
        this.Post(message, "", [||])

    member this.Post<'Tin>(message : 'Task, resultId:Guid) : unit = 
        this.Post(message, resultId.ToString("N"), [||])
    
    // TODO? public with Guid param?
    member internal this.Post<'Task>(message : 'Task, resultId : string, callerIds : string[]) : unit =
        if not (String.IsNullOrWhiteSpace(resultId)) then 
            resultWaiters.TryAdd(resultId, AsyncManualResetEvent()) |> ignore
        let envelope = message, resultId, callerIds
        let local = started //&& this.semaphor.CurrentCount > 0
        //Console.WriteLine(this.semaphor.CurrentCount.ToString())
        match local with
        | true ->
            //Debug.Print("Posted local message") 
            let pipelineId = Guid.NewGuid().ToString("N")
            // 1. if call is from outsider, any error is nonrecoverable since there is no rId
            // 2. if last step of continuation, pipeline is set during receive and continuator
            // knows how to recover if the last step dies
            // result is set with empty caller => means PaGR from outside, the only case
            // we need to save message to pipeline here
//            if (not (String.IsNullOrWhiteSpace(resultId))) && (callerIds.Length = 0) then
//                redis.HSet<'Task * string * string[]>(pipelineKey, pipelineId, envelope, When.Always, false) |> ignore
            messageQueue.Enqueue(envelope, pipelineId)
            messageWaiter.Set() |> ignore
        | _ -> // false
            //Debug.Print("Posted Redis message") 
            redis.LPush<'Task * string * string[]>(inboxKey, envelope) |> ignore
            // no resultId here because we notify recievers that in turn will notify callers about results (TODO? could use two channels - jobs and results)
            redis.Publish<string>(channelKey, "", true) |> ignore
    
    member this.GetResult(resultId : Guid) : Async<'TResult> =
        this.GetResult(resultId.ToString("N"), Timeout.Infinite, false)
    member this.GetResult(resultId : Guid, millisecondsTimeout) : Async<'TResult> =
        this.GetResult(resultId.ToString("N"), millisecondsTimeout, false)
    /// <summary>
    /// Returns result by known result identificator.
    /// </summary>
    /// <param name="resultId">Result guid that was set in Post method</param>
    /// <param name="millisecondsTimeout">Timeout after which TimeoutException exception will be thrown</param>
    /// <param name="keep">if true, result will remain cached in Redis until
    /// this method is called with keep = false</param>
    member this.GetResult(resultId : Guid, millisecondsTimeout, keep:bool) : Async<'TResult> =
        this.GetResult(resultId.ToString("N"), millisecondsTimeout, keep)

    // TODO TryGetResult

    member internal this.GetResult(resultId : string, millisecondsTimeout, keep:bool) : Async<'TResult> = 
        //Debug.Print("Getting: " + resultId)
        let cached = resultsCache.Get(resultId)
        if cached <> null then 
            if not keep then this.DeleteResult(resultId)
            async { return unbox cached }
        else
            let rec awaitResult tryCount = 
                async { 
                    Debug.Assert(resultWaiters.ContainsKey(resultId))
                    let waiter = resultWaiters.[resultId]
                    let cachedResult = resultsCache.Get(resultId)
                    let! result = 
                        if cachedResult <> null then async {return unbox cachedResult}
                        else redis.HGetAsync<'TResult>(resultsKey, resultId) |> Async.AwaitTask
                    if Object.Equals(result, null) then 
                        let! signal = waiter.WaitAsync() |> Async.AwaitTask // Async.AwaitWaitHandle(waitHandle, 100000)
                        if not signal then Debug.Print("Timeout in awaitResult") 
                        // TODO sould document that without timeout it is 60 minutes
                        if tryCount > 10 then Debug.Fail("Cannot receive result for PostAndReply" + resultId)
                        return! awaitResult (tryCount + 1)
                    else
                        resultWaiters.TryRemove(resultId) |> ignore
                        if not keep then this.DeleteResult(resultId)
                        return result
                }
            async { let! t = Async.StartChild(awaitResult 1, millisecondsTimeout)
                    return! t }

    member internal this.DeleteResult(resultId : string) : unit = 
        redis.HDel(resultsKey, resultId, true) |> ignore
        
    member this.PostAndGetResult(message : 'Task) : Async<'TResult> = 
        this.PostAndGetResult(message, "", Timeout.Infinite, [||])
    member this.PostAndGetResult(message : 'Task, millisecondsTimeout) : Async<'TResult> = 
        this.PostAndGetResult(message, "", millisecondsTimeout, [||])
    member internal this.PostAndGetResult(message : 'Task, resultId : string, millisecondsTimeout, callerIds : string array) : Async<'TResult> = 
        let resultId = 
            if String.IsNullOrWhiteSpace(resultId) then Guid.NewGuid().ToString("N")
            else resultId
        let envelope = message, resultId, callerIds
        let local = started //&& this.semaphor.CurrentCount > 0
        //Console.WriteLine(this.semaphor.CurrentCount.ToString())
        // !!! just check for semaphor/open gates right here and do calculations
        // right away. For local execution we should be as fast as MBP
        // for remote - 1ms pure async overhead is OK, because redis trips are longer
        // that way we could use the same API for all jobs - local and remote
        // actors abstraction remains, Redis is a nice remoting alternative
        // TODO instead of match use if and (started && semaphor.Wait(0)) - if cannot start 
        // right now push to redis
        match local with
        | true ->
            // TODO?? introduce "volatile" actors - those without pipeline & recovery
            // if they fail that is OK - next try will do it
            // that way we will save redis trips
            // volatile by default, so use "secure" or "reliable" words
            computation(message, resultId)
        | _ ->
            this.Post(message, resultId, callerIds)
            this.GetResult(resultId, millisecondsTimeout, false)
    
    // C# naming style and return type
    member this.PostAndGetResultAsync(message : 'Task) : Task<'TResult> = 
        this.PostAndGetResultAsync(message, Timeout.Infinite)
    member this.PostAndGetResultAsync(message : 'Task, millisecondsTimeout) : Task<'TResult> = 
        let res : Async<'TResult> = this.PostAndGetResult(message)
        res |> Async.StartAsTask
    
    // TODO
    member this.ContinueWith(continuation : Actor<'TResult, 'TCResult>) : Actor<'Task, 'TCResult> = 
        let id = this.Id + "->>-" + continuation.Id
        if Actor<_,_>.ActorsRepo.ContainsKey(id) then unbox Actor<_,_>.ActorsRepo.[id]
        else
            let redisConnStr = this.RedisConnectionString
            let lowPriority = false // continuation is cheap by itself

            let computation : 'Task * string -> Async<'TCResult> =
                fun message ->
                    async {
                        let task, resultId = message
                        this.Post(task, resultId+"_start", [|continuation.Id|])
                        // do not delete intermediate results untill the final result is saved
                        let! result = this.GetResult(resultId+"_start", Timeout.Infinite, true)
                        Debug.Print("First result: " + result.ToString())
                        continuation.Post(result, resultId+"_"+continuation.Id, [||])
                        // delete final result
                        let! cResult = continuation.GetResult(resultId+"_"+continuation.Id, Timeout.Infinite, false)
                        Debug.Print("Second result: " + cResult.ToString())
                        // delete intemediate result after finishing
                        this.DeleteResult(resultId+"_start")
                        return cResult
                    }
            let actor = new Actor<'Task, 'TCResult>(redisConnStr, id, computation, Timeout.Infinite, lowPriority)
            actor.semaphor <- this.semaphor
            actor.counter <- this.counter
            actor.lowPriorityGate <- this.lowPriorityGate
            Actor<_,_>.ActorsRepo.[id] <- box actor
            actor.Start()
            actor



    // TODO WaitFrom method??? Extension method for Tuples of Actors??
    // how to relaibly wait result from two+ actors? 
    // waitor and continuator are both actor
    // what if we just define some computation that will achieve that?
    // e.g. Post(..) could accept result id - that means that the result will be claimed later
    // so we Post(resultId) and then wait when we could claim the result
    // this will work for waiter and continuators in the same way
    // and we could avoid passing callerIds, callerRedises - only expected resultId
    // instead of callerId/Redis we use local cache and continuator 

    
    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            cts.Dispose()


