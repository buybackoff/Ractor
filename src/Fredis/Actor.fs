#nowarn "760" // new for IDisposable


namespace Fredis.FSharp
open System
[<AbstractClassAttribute>]
type ActorDefinition<'Task, 'TResult>() = 
    abstract RedisConnectionString : string
    override this.RedisConnectionString = ""
    abstract Computation : 'Task -> Async<'TResult>
    override this.Computation(input) = 
        async { return Unchecked.defaultof<'TResult>}
    /// <summary>
    /// Time in milliseconds to wait for computation to finish and to wait before discarding unclaimed results.
    /// </summary>
    abstract ResultTimeout : int
    override this.ResultTimeout = 60000
    abstract LowPriority : bool
    override this.LowPriority = false
    abstract AutoStart : bool
    override this.AutoStart = true
    //TODO explain this option later with full implications
    abstract Optimistic : bool
    override this.Optimistic = false

namespace Fredis

open System
open System.Linq
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Runtime.Caching
open System.Web.Hosting
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
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


// TODO make it internal and use only extension methods on definitions
[<AbstractClassAttribute>]
type ActorDefinition<'Task, 'TResult>() = 
    abstract RedisConnectionString : string
    override this.RedisConnectionString = ""
    abstract Computation : 'Task -> Task<'TResult>
    override this.Computation(input) = 
        let tcs = TaskCompletionSource()
        tcs.SetResult(Unchecked.defaultof<'TResult>)
        tcs.Task
    /// <summary>
    /// Time in milliseconds to wait for computation to finish and to wait before discarding unclaimed results.
    /// </summary>
    abstract ResultTimeout : int
    override this.ResultTimeout = 60000
    abstract LowPriority : bool
    override this.LowPriority = false
    abstract AutoStart : bool
    override this.AutoStart = true
    abstract Optimistic : bool
    override this.Optimistic = false


type internal Envelope<'Task> = 'Task * string * string []


type internal Actor<'Task, 'TResult> 
    internal (redisConnectionString : string, id : string, 
                computation : 'Task * string -> Async<'TResult>, resultTimeout : int, 
                lowPriority : bool, optimistic : bool) = 
    let redis = new Redis(redisConnectionString, "Fredis")
    let mutable started = false
    let mutable cts = new CancellationTokenSource()
    let messageWaiter = new AsyncAutoResetEvent()
    let localResultWaiters = ConcurrentDictionary<string, ManualResetEventSlim>()
    let prefix = "{" + id + "}" + ":Mailbox" // braces for Redis cluster, so all objects for an actor are on the same shard
    // list of incoming messages
    let inboxKey = prefix + ":inbox" // TODO message is a tuple of resultId * callerId * payload
    // hash of messages being processed
    let pipelineKey = prefix + ":pipeline"
    // hash of results not yet claimed by callers
    let resultsKey = prefix + ":results" // TODO results must have "for" property
    let channelKey = prefix + ":channel"
    let errorsKey = prefix + ":errors"
    // TODO
    [<DefaultValue>] val mutable internal errorHandler : Actor<ExceptionInfo<'Task>, unit> ref

    // this could be set from outside and block execution of low-priority tasks
    // could be used to guarantee execution of important task without waiting for autoscale
    // e.g. simple rule if CPU% > 80% for a minute then suspend low-priority actors
    // and resume when CPU% falls below 50%. If then we set autoscale rule at 65% 5-min
    // the autoscale group will grow only when high-priority tasks consume > 65% for several minutes
    static let mutable highPriorityGate = new ManualResetEventSlim(true)
    static let mutable lowPriorityGate = new ManualResetEventSlim(true)
    static let mutable counter = ref 0
    static let mutable loadMonitor = 
        // very simplictic counter just to offload LPs when there are too many tasks
        let maxThreads = Math.Min(Environment.ProcessorCount * 64, (fst (ThreadPool.GetMaxThreads())))
        //let activeThreads = (fst (ThreadPool.GetMaxThreads())) - (fst (ThreadPool.GetAvailableThreads()))  // counter instead
        { new IFredisPerformanceMonitor with
              member x.AllowHighPriorityActors() = !counter < maxThreads
              member x.AllowLowPriorityActors() = !counter < (maxThreads / 2)
              member x.PeriodMilliseconds = 1000 }
    let rec checkGates() =
        async {
            if loadMonitor.AllowHighPriorityActors() then highPriorityGate.Set()
            else highPriorityGate.Reset()
            if loadMonitor.AllowLowPriorityActors() then lowPriorityGate.Set()
            else lowPriorityGate.Reset()
            do! Async.Sleep(loadMonitor.PeriodMilliseconds)
            return! checkGates()
        }
    let waitForOpenGates timeout : Async<bool> = 
        async { 
            let! hp = Async.AwaitWaitHandle(highPriorityGate.WaitHandle, timeout)
            let! lp =
                if lowPriority then 
                    Async.AwaitWaitHandle(lowPriorityGate.WaitHandle, timeout)
                else async {return true}
            return hp && lp
        }
    let messageQueue = ConcurrentQueue<Envelope<'Task> * string>()
    static let actors = Dictionary<string, obj>()
    static let resultsCache = MemoryCache.Default

    do
        redis.Serializer <- Serialisers.Pickler
        checkGates() |> Async.Start

    static member LoadMonitor
        with get () = loadMonitor
        and set monitor = loadMonitor <- monitor
    static member val DefaultRedisConnectionString = "" with get, set
    static member ActorsRepo with get () = actors
    static member Instance<'Task, 'TResult>(definition:obj) : Actor<'Task, 'TResult> = 
        let name = definition.GetType().FullName
        if Actor<_,_>.ActorsRepo.ContainsKey(name) then 
                Actor<_,_>.ActorsRepo.[name] :?> Actor<'Task, 'TResult>
        else
            // code duplication is OK here, otherwise will need interface, etc... and still type matching
            match definition with
            | :? ActorDefinition<'Task, 'TResult> as taskDefinition -> 
                let conn = 
                    if String.IsNullOrWhiteSpace(taskDefinition.RedisConnectionString) then
                        if String.IsNullOrWhiteSpace(Actor<_,_>.DefaultRedisConnectionString) then
                            raise (new ArgumentException("Redis connection string is not set"))
                        else
                            Actor<_,_>.DefaultRedisConnectionString
                    else taskDefinition.RedisConnectionString
                let comp (msg:'Task * string) : Async<'TResult> = taskDefinition.Computation(fst msg) |> Async.AwaitTask
                Actor(conn, definition.GetType().Name, comp, taskDefinition.ResultTimeout, taskDefinition.LowPriority, taskDefinition.Optimistic)
            | :? Fredis.FSharp.ActorDefinition<'Task, 'TResult> as asyncDefinition ->
                let conn = 
                    if String.IsNullOrWhiteSpace(asyncDefinition.RedisConnectionString) then
                        if String.IsNullOrWhiteSpace(Actor<_,_>.DefaultRedisConnectionString) then
                            raise (new ArgumentException("Redis connection string is not set"))
                        else
                            Actor<_,_>.DefaultRedisConnectionString
                    else asyncDefinition.RedisConnectionString
                let comp (msg:'Task * string) : Async<'TResult> = asyncDefinition.Computation(fst msg)
                Actor(conn, definition.GetType().Name, comp, asyncDefinition.ResultTimeout, asyncDefinition.LowPriority, asyncDefinition.Optimistic)
            | _ -> failwith "wrong definition type"
    

    member internal this.Id = id
    member internal this.RedisConnectionString = redisConnectionString
    member internal this.Computation = computation
    member internal this.ResultTimeout = resultTimeout
    member internal this.LowPriority = lowPriority
    member internal this.Optimistic = optimistic

    member this.QueueLength = (int (redis.LLen(inboxKey))) + messageQueue.Count
    
    // public error handling should be C# friendly
    member this.ErrorHandler 
        with internal get () = !this.errorHandler
        and internal set (eh) = this.errorHandler := eh
    
    member this.Start() : unit = 
        if not started then 
            HostingEnvironment.RegisterObject(this)
            let rec awaitMessage() = 
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
                        let! message = redis.EvalAsync<Envelope<'Task>>
                                                (lua, 
                                                [|  redis.KeyNameSpace + ":" + inboxKey
                                                    redis.KeyNameSpace + ":" + pipelineKey
                                                    pipelineId |])
                                       |> Async.AwaitTask
                        if Object.Equals(message, Unchecked.defaultof<Envelope<'Task>>) then 
                            let! signal = messageWaiter.WaitAsync(1000) |> Async.AwaitTask // TODO timeout, if PubSub dropped notification, recheck the queue, but not very often
                            if not signal then Debug.Print("Timeout in awaitMessage")
                            return! awaitMessage()
                        else 
                            //Debug.Print("Took Redis message") 
                            return message, pipelineId
                }
            
            redis.Subscribe(channelKey, 
                            Action<string, string>(fun channel messageNotification -> 
                                match messageNotification with
                                | "" -> messageWaiter.Set() |> ignore
                                | resultId -> 
                                    if localResultWaiters.ContainsKey(resultId) then 
                                        //Debug.Print("Setting result handle: " + resultId)
                                        localResultWaiters.[resultId].Set() |> ignore
                                    else Debug.Print "Unexpected result id" // TODO test this condition
                                )
                            )

            cts <- new CancellationTokenSource()
            let loop = 
                async { 
                    while (not cts.Token.IsCancellationRequested) do
                        //Debug.Print("Before gate")
                        let! opened = waitForOpenGates Timeout.Infinite
                        Debug.Assert(opened)
                        let! (message, resultId, callerIds), pipelineId = awaitMessage()
                        //Debug.Print("Received message: " + resultId)
                        async { 
                            try 
                                Interlocked.Increment(counter) |> ignore
                                try 
                                    let! child = Async.StartChild(computation (message, resultId), this.ResultTimeout)
                                    let! result = child
                                    //Debug.Print("Completed computation for: " + resultId)
                                    
                                    // notify local waiter
                                    if localResultWaiters.ContainsKey(resultId) then
                                        // save trip to redis to get the result
                                        resultsCache.Add(resultId, result, 
                                            DateTimeOffset.Now.AddMilliseconds(float this.ResultTimeout)) 
                                            |> ignore
                                        // save result even though doing locally    
                                        if not this.Optimistic then // TODO not sure about logic here and in the whole try block
                                            do! redis.HSetAsync(resultsKey, resultId, result, When.Always, false)
                                                |> Async.AwaitTask
                                                |> Async.Ignore
                                        localResultWaiters.[resultId].Set() |> ignore
                                        
                                    else
                                        do! redis.HSetAsync(resultsKey, resultId, result, When.Always, false)
                                                |> Async.AwaitTask
                                                |> Async.Ignore
                                        redis.Publish<string>(channelKey, resultId, this.Optimistic) |> ignore
                                    redis.HDel(pipelineKey, pipelineId, this.Optimistic) |> ignore
                                with e -> 
                                    // TODO rework this
                                    let ei = ExceptionInfo(id, message, e)
                                    redis.LPush<ExceptionInfo<'Task>>(errorsKey, ei, When.Always, this.Optimistic) |> ignore
                            //if this.errorHandler <> Unchecked.defaultof<Actor<ExceptionInfo<'Task>, _> ref> then 
                            //    this.errorHandler.Value.Post(ei)
                            finally
                                Interlocked.Decrement(counter) |> ignore
                        }
                        //this.semaphor.Release() |> ignore
                        |> Async.Start // do as many task as global limits (semaphor, priority gate) alow 
                }
            Async.Start(loop, cts.Token)
            started <- true
    
    member this.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
        HostingEnvironment.UnregisterObject(this)
    
    //#region Public Post methods

    /// <summary>
    /// Post message and get its assigned result Guid after the message was saved in Redis.
    /// </summary>
    /// <param name="message">Payload</param>
    member this.Post<'Task>(message : 'Task) : Guid = 
        this.Post(message)
    member this.TryPost<'Task>(message : 'Task, [<Out>] resultGuid : byref<Guid>) : bool = 
        this.TryPost(message, &resultGuid)
    /// <summary>
    /// Post message and get its assigned result Guid after the message was saved in Redis.
    /// </summary>
    /// <param name="message">Payload</param>
    member this.PostTask<'Task>(message : 'Task) : Task<Guid> = 
        this.PostTask(message)
    member this.TryPostTask<'Task>(message : 'Task) : Task<bool*Guid> = 
        this.TryPostTask(message)
    member this.PostAsync<'Task>(message : 'Task) : Async<Guid> = 
        this.PostAsync(message)
    member this.TryPostAsync<'Task>(message : 'Task) : Async<bool*Guid> = 
        async {
            try
                let! result = this.Post(message, Guid.NewGuid(), [||])
                return true, result
            with
            | _ -> return false, Unchecked.defaultof<Guid>
        }
    
    //#endregion

    member private this.Post<'Task>(message : 'Task, resultGuid : Guid, callerIds : string []) : Async<Guid> = 
        let resultId = resultGuid.ToString("N")
        let envelope : Envelope<'Task> = message, resultId, callerIds
        let remotePost = 
            //Debug.Print("Posted Redis message") 
            let res = 
                async {
                    do! redis.LPushAsync<Envelope<'Task>>(inboxKey, envelope, When.Always, this.Optimistic) 
                                |> Async.AwaitTask |> Async.Ignore
                    return resultGuid
                }
            // no resultId here because we notify recievers to process a message and they in turn will notify 
            // callers about results
            redis.Publish<string>(channelKey, "", this.Optimistic) |> ignore
            res
        let localPost = 
            //Debug.Print("Posted local message")
            localResultWaiters.TryAdd(resultId, ManualResetEventSlim()) |> ignore 
            let pipelineId = Guid.NewGuid().ToString("N")
            if not this.Optimistic then 
                redis.HSet<Envelope<'Task>>(pipelineKey, pipelineId, envelope, When.Always, false) |> ignore
            messageQueue.Enqueue(envelope, pipelineId)
            messageWaiter.Set() |> ignore
            async {return resultGuid}
        match started with
        | true -> 
            async {
                let! opened = waitForOpenGates 0
                if opened then return! localPost
                else return! remotePost
            }    
        | _ -> remotePost
            

    
    /// <summary>
    /// Returns result by known result id.
    /// </summary>
    /// <param name="resultId">Result guid that was returned from a Post method</param>
    member this.GetResult(resultGuid : Guid) : 'TResult = 
        this.GetResultAsync(resultGuid) |> Async.RunSynchronously
    member this.TryGetResult(resultGuid : Guid, [<Out>] result : byref<'TResult>) : bool = 
        try
            result <- this.GetResultAsync(resultGuid) |> Async.RunSynchronously
            true
        with
        | _ -> 
            result <- Unchecked.defaultof<'TResult>
            false
    member this.GetResultTask(resultGuid : Guid) : Task<'TResult> = 
        this.GetResultAsync(resultGuid) |> Async.StartAsTask
    member this.TryGetResultTask(resultGuid : Guid) : Task<bool*'TResult> = 
        this.TryGetResultAsync(resultGuid) |> Async.StartAsTask

    member this.GetResultAsync(resultGuid : Guid) : Async<'TResult> = 
        let resultId = resultGuid.ToString("N")
        //Debug.Print("Getting: " + resultId)
        let cached = resultsCache.Get(resultId)
        if cached <> null then 
            this.DeleteResult(resultId)
            async { return unbox cached }
        else 
            let rec awaitResult tryCount = 
                async { 
                    // TODO review timeout logic
                    localResultWaiters.TryAdd(resultId, ManualResetEventSlim()) |> ignore 
                    let waiter = localResultWaiters.[resultId]
                    let cachedResult = resultsCache.Get(resultId)
                    let! result = if cachedResult <> null then async { return unbox cachedResult }
                                  else redis.HGetAsync<'TResult>(resultsKey, resultId) |> Async.AwaitTask
                    if Object.Equals(result, null) then 
                        let! signal = Async.AwaitWaitHandle(waiter.WaitHandle, 1000) // TODO proper to here
                        if not signal then Debug.Print("Timeout in awaitResult")
                        // TODO sould document that without timeout it is 60 minutes
                        if tryCount > 10 then Debug.Fail("Cannot receive result for PostAndReply" + resultId)
                        return! awaitResult (tryCount + 1)
                    else 
                        localResultWaiters.TryRemove(resultId) |> ignore
                        this.DeleteResult(resultId)
                        return result
                }
            async { let! t = Async.StartChild(awaitResult 1, this.ResultTimeout)
                    return! t }

    member this.TryGetResultAsync(resultGuid : Guid) : Async<bool*'TResult> = 
        async {
            try
                let! result = this.GetResultAsync(resultGuid)
                return true, result
            with
            | _ -> return false, Unchecked.defaultof<'TResult>
        }

    
    // TODO do we need to delete results manually (additional command per each result) or clean stale results
    // in a periodic script - do both, then measure what is gain without manual delete of each item
    [<ObsoleteAttribute>]
    member private this.DeleteResult(resultId : string) : unit = 
        redis.HDel(resultsKey, resultId, true) |> ignore


    member this.PostAndGetResult(message : 'Task) : 'TResult = 
        this.PostAndGetResultAsync(message) |> Async.RunSynchronously
    member this.PostAndGetResultTask(message : 'Task) : Task<'TResult> = 
        this.PostAndGetResultAsync(message) |> Async.StartAsTask
    member this.PostAndGetResultAsync(message : 'Task) : Async<'TResult> = 
        this.PostAndGetResult(message, Guid.NewGuid(), [||]) // TODO optimistic signatures

    member private this.PostAndGetResult(message : 'Task, resultGuid : Guid, callerIds : string array) : Async<'TResult> = 
        let resultId =  resultGuid.ToString("N")
        //let envelope : Envelope<'Task> = message, resultId, callerIds
        let standardCall = async {
                do! this.Post(message, resultGuid, callerIds) |> Async.Ignore
                return! this.GetResultAsync(resultGuid)
            }
        let shortcutCall = // avoid most of the async machinery
            async {
                try 
                    Interlocked.Increment(counter) |> ignore
                    // TODO ChildTask to save to pipeline if cautious
                    let! child = Async.StartChild(computation (message, resultId), this.ResultTimeout)
                    let! result = child
                    if not this.Optimistic then // TODO not sure about logic here and in the whole try block
                            do! redis.HSetAsync(resultsKey, resultId, result, When.Always, false)
                                |> Async.AwaitTask
                                |> Async.Ignore
                    return result
                finally
                    Interlocked.Decrement(counter) |> ignore
            }
        match started with
        | true -> 
            async {
                let! opened = waitForOpenGates 0
                if opened then return! shortcutCall
                else return! standardCall
            }           
        | _ -> standardCall    

    
    // TODO
//    member this.ContinueWith(continuation : Actor<'TResult, 'TCResult>) : Actor<'Task, 'TCResult> = 
//        let id = this.Id + "->>-" + continuation.Id
//        if Actor<_, _>.ActorsRepo.ContainsKey(id) then unbox Actor<_, _>.ActorsRepo.[id]
//        else 
//            let redisConnStr = this.RedisConnectionString
//            let lowPriority = false // continuation is cheap by itself
//            
//            let computation : 'Task * string -> Async<'TCResult> = 
//                fun message -> 
//                    async { 
//                        let task, resultId = message
//                        this.Post(task, resultId + "_start", [| continuation.Id |])
//                        // do not delete intermediate results untill the final result is saved
//                        let! result = this.GetResult(resultId + "_start", Timeout.Infinite, true)
//                        Debug.Print("First result: " + result.ToString())
//                        continuation.Post(result, resultId + "_" + continuation.Id, [||])
//                        // delete final result
//                        let! cResult = continuation.GetResult(resultId + "_" + continuation.Id, Timeout.Infinite, false)
//                        Debug.Print("Second result: " + cResult.ToString())
//                        // delete intemediate result after finishing
//                        this.DeleteResult(resultId + "_start")
//                        return cResult
//                    }
//            
//            let actor = new Actor<'Task, 'TCResult>(redisConnStr, id, computation, Timeout.Infinite, lowPriority)
//            actor.semaphor <- this.semaphor
//            actor.counter <- this.counter
//            actor.lowPriorityGate <- this.lowPriorityGate
//            Actor<_, _>.ActorsRepo.[id] <- box actor
//            actor.Start()
//            actor
    
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
    
    interface IRegisteredObject with
        member x.Stop(immediate : bool) = x.Stop()


// convenient way to use actors via extension methods on definitions
// TODO continuation extensions
// TODO copy docs comments from Actors
[<Extension>]
type ActorDefinitionExtension<'Task, 'TResult> () =
    [<Extension>]
    static member Post<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task) : Guid =
        let actor = Actor<_,_>.Instance(this)
        actor.Post(message)
    [<Extension>]
    static member Post<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task, fireAndForget : bool) : Guid = 
        let actor = Actor<_,_>.Instance(this)
        actor.Post(message, fireAndForget)
    [<Extension>]
    static member PostAsync<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task) : Task<Guid> =
        let actor = Actor<_,_>.Instance(this)
        actor.PostTask(message)
    [<Extension>]
    static member PostAsync<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task, fireAndForget : bool) : Task<Guid> = 
        let actor = Actor<_,_>.Instance(this)
        actor.PostTask(message, fireAndForget)
//    [<Extension>]
//    static member GetResult(this : ActorDefinition<'Task, 'TResult>, resultId : Guid) : Async<'TResult> = 
//        let actor = Actor<_,_>.Instance(this)
//        actor.GetResult(resultId.ToString("N"), Timeout.Infinite, false)
//    [<Extension>]
//    static member GetResult(this : ActorDefinition<'Task, 'TResult>, resultId : Guid, millisecondsTimeout) : Async<'TResult> = 
//        let actor = Actor<_,_>.Instance(this)
//        actor.GetResult(resultId.ToString("N"), millisecondsTimeout, false)



namespace Fredis.FSharp
open System
open Fredis
open Fredis.FSharp
open System.Threading
open System.Runtime.CompilerServices

[<Extension>]
type ActorDefinitionExtension<'Task, 'TResult> () =
    [<Extension>]
    static member Post<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task) : Guid =
        let actor = Actor<_,_>.Instance(this)
        actor.Post(message)
    [<Extension>]
    static member Post<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task, fireAndForget : bool) : Guid = 
        let actor = Actor<_,_>.Instance(this)
        actor.Post(message, fireAndForget)
//    [<Extension>]
//    static member PostAsync<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task) : Async<Guid> =
//        let actor = Actor<_,_>.Instance(this)
//        let resultGuid = Guid.NewGuid()
//        actor.Post(message, resultGuid, [||], false)
//    [<Extension>]
//    static member PostAsync<'Task>(this : ActorDefinition<'Task, 'TResult>, message : 'Task, fireAndForget : bool) : Async<Guid> = 
//        let actor = Actor<_,_>.Instance(this)
//        let resultGuid = Guid.NewGuid()
//        actor.Post(message, resultGuid, [||], fireAndForget)
//
//    [<Extension>]
//    static member GetResult(this : ActorDefinition<'Task, 'TResult>, resultId : Guid) : Async<'TResult> = 
//        let actor = Actor<_,_>.Instance(this)
//        actor.GetResult(resultId.ToString("N"), Timeout.Infinite, false)
//    [<Extension>]
//    static member GetResult(this : ActorDefinition<'Task, 'TResult>, resultId : Guid, millisecondsTimeout) : Async<'TResult> = 
//        let actor = Actor<_,_>.Instance(this)
//        actor.GetResult(resultId.ToString("N"), millisecondsTimeout, false)