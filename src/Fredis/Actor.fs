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
    
    static let actors = Dictionary<string, obj>()
    static member internal ActorsRepo with get () = actors
    
    static let resultsCache = MemoryCache.Default
    static let messageQueue = ConcurrentQueue<('Task * string) * string>()

    member private this.Id = id
    member private this.Redis = redis
    member private this.Computation = computation
    member this.QueueLength = (int (redis.LLen(inboxKey))) + messageQueue.Count
    
    // public error handling should be C# friendly
    member this.ErrorHandler 
        with internal get () = !this.errorHandler
        and internal set (eh) = this.errorHandler := eh
    
    member this.Start() : unit = 
        if not started then 
            let rec awaitMessage () = 
                async { 
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
                        Debug.Print("Took local message")
                        return localMessage
                    else
                        let! message = redis.EvalAsync<'Task * string>(lua, 
                                                                    [| redis.KeyNameSpace + ":" + inboxKey
                                                                       redis.KeyNameSpace + ":" + pipelineKey
                                                                       pipelineId |])
                                       |> Async.AwaitTask
                        if Object.Equals(message, Unchecked.defaultof<'Task * string>) then
                            Async.AwaitWaitHandle(awaitMessageHandle, 5000) |> ignore // if PubSub dropped notification, recheck the queue, but not very often
                            return! awaitMessage()
                        else
                            Debug.Print("Took Redis message") 
                            return message, pipelineId
                }
            
            let waitLowPriorityGateIsOpen = 
                async { 
                    if lowPriority && this.lowPriorityGate <> Unchecked.defaultof<ManualResetEventSlim> then 
                        do! Async.AwaitWaitHandle(this.lowPriorityGate.WaitHandle) |> Async.Ignore
                }
            
            redis.Subscribe(channelKey, 
                            Action<string, string>(fun channel messageNotification -> 
                                match messageNotification with
                                | "" -> awaitMessageHandle.Set() |> ignore
                                | resultId -> 
                                    if expectedResultsHandles.ContainsKey(resultId) then 
                                        expectedResultsHandles.[resultId].Set() |> ignore
                                    else failwith "wrong result id"))
            cts <- new CancellationTokenSource()
            let loop = 
                async { 
                    while (not cts.Token.IsCancellationRequested) do
                        do! this.semaphor.WaitAsync(cts.Token)
                            |> Async.AwaitIAsyncResult
                            |> Async.Ignore
                        do! waitLowPriorityGateIsOpen
                        try 
                            
                            let! (message, resultId), pipelineId = awaitMessage()
                            async {
                                try 
                                    let! result = computation message
                                    // TODO add to local cache with sliding policy for 5 sec
                                    // trace cache hits and test performance with and without it
                                    if resultId <> "" then // then someone is waiting for the result
                                        // if started 
                                        redis.HSet(resultsKey, resultId, result, When.Always, false) |> ignore
                                        redis.Publish<string>(channelKey, resultId, true) |> ignore
                                    redis.HDel(pipelineKey, pipelineId, true) |> ignore
                                with e -> 
                                    let ei = ExceptionInfo(id, message, e)
                                    redis.LPush<ExceptionInfo<'Task>>(errorsKey, ei, When.Always, true) |> ignore
                                    if this.errorHandler.Value <> Unchecked.defaultof<Actor<ExceptionInfo<'Task>, _>> then 
                                        this.errorHandler.Value.Post(ei)
                            }
                            |> Async.Start // do as many task as global limits (semaphor, priority gate) alow 
                        finally
                            this.semaphor.Release() |> ignore
                }
            Async.Start(loop, cts.Token)
            started <- true
    
    member this.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
    
    // then all other methods are jsut combinations of those
    member this.Post<'Tin>(message : 'Task) : unit = 
        this.Post(message, "", "", null)
    
    // TODO? public with Guid param?
    member internal this.Post<'Task>(message : 'Task, resultId : string, callerId : string, callerRedis : Redis) : unit =
        if not (String.IsNullOrWhiteSpace(resultId)) then 
            expectedResultsHandles.Add(resultId, new AutoResetEvent(false))
        match started with
        | true ->
            Debug.Print("Posted local message") 
            let pipelineId = Guid.NewGuid().ToString("N")
            let ok = redis.HSet<'Task>(pipelineKey, pipelineId, message, When.Always, false) // save message, wait
            messageQueue.Enqueue((message, resultId), pipelineId)
            awaitMessageHandle.Set() |> ignore
        | _ -> // false
            Debug.Print("Posted Redis message") 
            redis.LPush<'Task * string>(inboxKey, (message, resultId)) |> ignore
            // no resultId here because we notify recievers that in turn will notify callers about results (TODO? could use two channels - jobs and results)
            redis.Publish<string>(channelKey, "", true) |> ignore

    // TODO TryGetResult(out) for C#

    member internal this.GetResult(resultId : string) : Async<'TResult> = 
        this.GetResult(resultId, Timeout.Infinite)

    member internal this.GetResult(resultId : string, millisecondsTimeout : int) : Async<'TResult> = 
        let cached = resultsCache.Get(resultId)
        if cached <> null then async { return unbox cached }
        else
            let rec awaitResult tryCount = 
                async { 
                    Debug.Assert(expectedResultsHandles.ContainsKey(resultId))
                    let waitHandle = expectedResultsHandles.[resultId]
                    
                    // expect that notification will come for expected result id
                    // do not trust PubSub yet, in Redis docs they say messages could be lost
                    // divide given timeout by 3 to check results set before the given timeout
                    let timeout = 
                        if millisecondsTimeout = Timeout.Infinite then 1000
                        else millisecondsTimeout / 3
                    let! signal = Async.AwaitWaitHandle(waitHandle, timeout)
                    if signal then 
                        let cachedResult = resultsCache.Get(resultId)
                        let! result = 
                            if cachedResult <> null then async {return unbox cachedResult}
                            else redis.HGetAsync<'TResult>(resultsKey, resultId) |> Async.AwaitTask
                        if Object.Equals(result, null) then 
                            Debug.Fail("Expected to get the right result after the signal")
                        Debug.Assert(expectedResultsHandles.ContainsKey(resultId))
                        expectedResultsHandles.[resultId].Dispose()
                        expectedResultsHandles.Remove(resultId) |> ignore
                        redis.HDel(resultsKey, resultId, true) |> ignore
                        return result
                    else // timeout
                        // TODO sould document that without timeout it is 30 minutes
                        if tryCount > 1800 then Debug.Fail("Cannot receive result for PostAndReply")
                        return! awaitResult (tryCount + 1)
                }
            async { let! t = Async.StartChild(awaitResult 1, millisecondsTimeout)
                    return! t }
        

    member this.PostAndGetResult(message : 'Task) : Async<'TResult> = 
        this.PostAndGetResult(message, Timeout.Infinite, "", "", null)

    member this.PostAndGetResult(message : 'Task, millisecondsTimeout) : Async<'TResult> = 
        this.PostAndGetResult(message, millisecondsTimeout, "", "", null)
    
    // TODO make internal method, resultId and continuation to be used with ContinueWith
    //abstract PostAndReply :'Task * int * string *  Redis * string -> Async<'TResult>
    member this.PostAndGetResult(message : 'Task, millisecondsTimeout, resultId : string, callerId : string, callerRedis : Redis) : Async<'TResult> = 
        let resultId = 
            if String.IsNullOrWhiteSpace(resultId) then Guid.NewGuid().ToString("N")
            else resultId
        this.Post(message, resultId, callerId, callerRedis)
        this.GetResult(resultId, millisecondsTimeout)
    
    
    member this.PostAndGetResultAsync(message : 'Task) : Task<'TResult> = this.PostAndGetResultAsync(message, Timeout.Infinite)
    
    // C# naming style and return type
    member this.PostAndGetResultAsync(message : 'Task, millisecondsTimeout) : Task<'TResult> = 
        let res : Async<'TResult> = this.PostAndGetResult(message, millisecondsTimeout)
        res |> Async.StartAsTask
    
    // TODO
    member this.ContinueWith(continuation : Actor<'TResult, 'TCResult>) : Actor<'Task, 'TCResult> = 
        let id = this.Id + "->>-" + continuation.Id
        if Actor<_,_>.ActorsRepo.ContainsKey(id) then unbox Actor<_,_>.ActorsRepo.[id]
        else
            let redis = this.Redis
            let lowPriority = false // continuation is cheap by itself

            let computation : 'Task -> Async<'TCResult> =
                let resulId = Guid.NewGuid().ToString("N")
                fun task ->
                    async {
                        this.Post(task, resulId, continuation.Id, continuation.Redis)
                        let! result = this.GetResult(resulId)
                        return! Unchecked.defaultof<Async<'TCResult>>
                    }
            
                // Post() with resultId
                // Wait for result
                // atomically move result from this to continuation
                // start continuation
                // cache result for continuation to be able to check it if it works on the same machine
            

            let actor = new Actor<'Task, 'TCResult>(redis, id, computation, lowPriority)
            Actor<_,_>.ActorsRepo.[id] <- box actor
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
            awaitMessageHandle.Dispose()
            cts.Dispose()


