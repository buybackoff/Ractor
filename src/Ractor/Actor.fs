#nowarn "760" // new for IDisposable

// TODO move design todos here from all scattered comments

// ResultTimeout is used for computation and garbage collection as well
// but remember that results will stay in Redis for that time and if it is very long
// the memory consumption could be large. Possible solutions:
// - multiple, e.g. computation timeout is 10x of results
// - no meaningful timeout for computations, do the logic inside actors if needed
// - leave it as is
// - TODO: make RTO => CalcTO - an emergency setting for calcs. Monitor how fast each result
// is reclaimed *locally* (no trips to Redis for logging) and schedule GC with a multiple of that
// or with 5-sigma, etc of that e.g. if on average we reclaim in 1 sec, store for 10 seconds

// TODO attributes instead of abstract methods + ActorBase, keep all properties but assign in constructor according to attrs

// TODO Post() must handle errors, e.g. via GetResult in an async task
// TODO!! do not drop messages when computation errored but put them back to the queue
// and log/notify

// NOTE: one of actor's property (from wiki) is behavior specified as a mathematical function to 
// express what an Actor does when it processes a message including specifying a new 
// behavior to process the next message that arrives.
// The last part basically means state - that could be done by directly accessing Redis 
// inside actors (e.g. counters) or use static distributed data structures in actor definition 
// (the same stuff, but more abstracted).

// TODO make twi overloadable methods - one in not async, async is optional


namespace Ractor
open System
[<AbstractClassAttribute>]
type ActorBase() = 
    static member val Logger = Logging.Silent with get, set

namespace Ractor.FSharp
open System
open Ractor
// TODO? PreserveOrder option is possible - is it needed?
// should lock inbox while executing a computation
// and unlock upon returning its result
[<AbstractClassAttribute>]
type Actor<'Task, 'TResult>() as this = 
    inherit ActorBase()
    let mutable extendedComputation : Message<'Task> * string -> Async<Message<'TResult>> = 
        fun (inMessage,_) -> 
            async {
                if inMessage.HasError then return Message(Unchecked.defaultof<'TResult>,true,inMessage.Error) 
                else
                    let task = inMessage.Value
                    try
                        let! child = Async.StartChild(this.Computation(task), this.ResultTimeout)
                        let! result = child
                        return Message(result, false, null)
                    with e -> 
                        ActorBase.Logger.Error("Computation error", Some(e))
                        return Message(Unchecked.defaultof<'TResult>,true,e)
            }
    abstract Redis : string with get
    override this.Redis = "localhost"
    /// <summary>
    /// One actor implementation instance per id.
    /// </summary>
    abstract InstanceId : string with get, set
    override val InstanceId = "" with get, set
    abstract Computation : 'Task -> Async<'TResult>
    override this.Computation(input) = 
        async { return Unchecked.defaultof<'TResult>}
    /// <summary>
    /// Time in milliseconds to wait for computation to finish and to wait before discarding unclaimed results.
    /// </summary>
    abstract ResultTimeout : int with get
    override this.ResultTimeout with get() =  60000
    abstract LowPriority : bool with get
    override this.LowPriority with get() =  false
    abstract AutoStart : bool with get
    override this.AutoStart with get() = true
    abstract Optimistic : bool with get
    override this.Optimistic with get() = false
    abstract GetKey : unit -> string
    override this.GetKey() = this.GetType().FullName + (if String.IsNullOrEmpty(this.InstanceId) then "" else ":" + this.InstanceId)
    // extended computation for continuations
    member internal this.ExtendedComputation 
        with get () = extendedComputation
        and set v = extendedComputation <- v

namespace Ractor

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
open Ractor


[<AbstractClassAttribute>]
type Actor<'Task, 'TResult>() as this = 
    inherit ActorBase()
    let mutable extendedComputation : Message<'Task> * string -> Async<Message<'TResult>> = 
        fun (inMessage,_) -> 
            async {
                if inMessage.HasError then return Message(Unchecked.defaultof<'TResult>,true,inMessage.Error) 
                else
                    let task = inMessage.Value
                    try
                        let! child = Async.StartChild(this.Computation(task) |> Async.AwaitTask, this.ResultTimeout)
                        let! result = child
                        return Message(result, false, null)
                    with e -> 
                        ActorBase.Logger.Error("Computation error", Some(e))
                        return Message(Unchecked.defaultof<'TResult>, true, e)
            }

    abstract Redis : string with get
    override this.Redis with get() =  "localhost" 
    /// <summary>
    /// One actor implementation instance per id.
    /// </summary>
    abstract InstanceId : string with get,set
    override val InstanceId = "" with get,set
    abstract Computation : 'Task -> Task<'TResult>
    override this.Computation(input) = 
        let tcs = TaskCompletionSource()
        tcs.SetResult(Unchecked.defaultof<'TResult>)
        tcs.Task
    /// <summary>
    /// Time in milliseconds to wait for computation to finish and to wait before discarding unclaimed results.
    /// </summary>
    abstract ResultTimeout : int with get
    override this.ResultTimeout with get() =  60000
    abstract LowPriority : bool with get
    override this.LowPriority with get() =  false
    abstract AutoStart : bool with get
    override this.AutoStart with get() = true
    abstract Optimistic : bool with get
    override this.Optimistic with get() = false
    abstract GetKey : unit -> string
    override this.GetKey() = this.GetType().FullName + (if String.IsNullOrEmpty(this.InstanceId) then "" else ":" + this.InstanceId)
    // extended computation for continuations
    member internal this.ExtendedComputation 
        with get () = extendedComputation
        and set v = extendedComputation <- v


type internal ActorImpl<'Task, 'TResult> 
    internal (redisConnectionString : string, id : string, 
                computation : Message<'Task> * string -> Async<Message<'TResult>>, resultTimeout : int, 
                lowPriority : bool, autoStart : bool, optimistic : bool) as this = 
    let redis = new Redis(redisConnectionString, "R")
    let garbageCollectionPeriod = resultTimeout
    let mutable started = false
    let mutable cts = new CancellationTokenSource()
    let messageWaiter = new AsyncAutoResetEvent()
    let localResultListeners = ConcurrentDictionary<string, ManualResetEventSlim>()
    let prefix = "{" + id + "}" // + ":Mailbox" // braces for Redis cluster, so all objects for an actor are on the same shard
    // list of incoming messages
    let inboxKey = prefix + ":inbox" // TODO message is a tuple of resultId * callerId * payload
    // hash of messages being processed
    let pipelineKey = prefix + ":pipeline"    // prefix of results not yet claimed by callers
    let resultsKey = prefix + ":results" // TODO results must have "for" property
    let channelKey = prefix + ":channel"
    let errorsKey = prefix + ":errors"
    let lockKey = prefix + ":lock"

    // HINCR acttor id on start and decr on stop
    static let runningActorsHash = "runningActors"

    // this could be set from outside and block execution of low-priority tasks
    // could be used to guarantee execution of important task without waiting for autoscale
    // e.g. simple rule if CPU% > 80% for a minute then suspend low-priority actors
    // and resume when CPU% falls below 50%. If we set autoscale rule at 65% 5-min
    // the autoscale group will grow only when high-priority tasks consume > 65% for several minutes
    static let mutable highPriorityGate = new ManualResetEventSlim(true)
    static let mutable lowPriorityGate = new ManualResetEventSlim(true)
    static let mutable counter = ref 0
    static let mutable performanceMonitor = 
        // very simplictic counter just to offload LPs when there are too many tasks
        let maxThreads = Math.Min(Environment.ProcessorCount * 64, (fst (ThreadPool.GetMaxThreads())))
        //let activeThreads = (fst (ThreadPool.GetMaxThreads())) - (fst (ThreadPool.GetAvailableThreads()))  // counter instead
        { new IRactorPerformanceMonitor with
              member x.AllowHighPriorityActors() = true // !counter < maxThreads
              member x.AllowLowPriorityActors() = true  // !counter < (maxThreads / 2)
              member x.PeriodMilliseconds = 1000 }
    let rec checkGates() =
        async {
            if performanceMonitor.AllowHighPriorityActors() then highPriorityGate.Set()
            else highPriorityGate.Reset()
            if performanceMonitor.AllowLowPriorityActors() then lowPriorityGate.Set()
            else lowPriorityGate.Reset()
            do! Async.Sleep(performanceMonitor.PeriodMilliseconds)
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

    // Global cache reference
    let cache = Redis.Cache

    static let actors = Dictionary<string, obj>()

    let start() =
        if not started then 
            HostingEnvironment.RegisterObject(this)
            let rec awaitMessage() = 
                async { 
                    //Debug.Print("Awaiting message")
                    // move to safe place while processing
                    let lua = @"
                    local result = redis.call('RPOP', KEYS[1])
                    if result ~= nil then
                        redis.call('HSET', KEYS[2], KEYS[3], result)
                    end
                    return result"
                    let pipelineId = Guid.NewGuid().ToBase64String()
                    let hasLocal, localMessage = messageQueue.TryDequeue()
                    if hasLocal then 
                        Debug.Print("Took local message")
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
                            if not signal then Debug.Print("Timeout in awaitMessage in: " + this.Id)
                            return! awaitMessage()
                        else 
                            Debug.Print("Took Redis message") 
                            return message, pipelineId
                }
            
            redis.Subscribe(channelKey, 
                            Action<string, string>(fun channel messageNotification -> 
                                match messageNotification with
                                | "" -> messageWaiter.Set() |> ignore
                                | resultId -> 
                                    if localResultListeners.ContainsKey(resultId) then 
                                        //Debug.Print("Setting result handle: " + resultId)
                                        localResultListeners.[resultId].Set() |> ignore
                                    else
                                        ()
                                    // 1. we get all result ids and we must cache
                                    // all results that we haven't explicitly waited
                                    // by doing so, we could be safe with the first loop in 
                                    // the result getter: local execution set MRE explicitly, while the cache 
                                    // will tell us if there was a notification but we missed it.
                                    // 2. by caching ids that we waited, we could reclaim a result
                                    // after a worker death without re-posting a task (TryGetResultImmediate method)
                                    cache.Add(resultsKey + ":id:" + resultId, Object(), 
                                        DateTimeOffset.Now.AddMilliseconds(float this.ResultTimeout)) 
                                        |> ignore
                                )
                            )

            cts <- new CancellationTokenSource()
            let loop = 
                async { 
                    while (not cts.Token.IsCancellationRequested) do
                        //Debug.Print("Before gate")
                        let! opened = waitForOpenGates Timeout.Infinite
                        Debug.Assert(opened)
                        let! envelope, pipelineId = awaitMessage()
                        let (inMessage, resultId, callerIds) = envelope.Message, envelope.ResultId, envelope.CallerIds
                        //Debug.Print("Received message: " + resultId)
                        async { 
                            try 
                                Interlocked.Increment(counter) |> ignore

                                let! child = Async.StartChild(computation (inMessage, resultId), this.ResultTimeout)
                                let! outMessage = child


                                // NEW LOGIC
                                // first check if there are caller ids
                                if Array.isEmpty callerIds then
                                    // if empty, notify result waiters
                                    // notify local waiter if it exists
                                    if localResultListeners.ContainsKey(resultId) then
                                        // save result and notify others about it
                                        // save trip to redis to get the result
                                        cache.Set(resultsKey + ":" + resultId, outMessage, 
                                            DateTimeOffset.Now.AddMilliseconds(float this.ResultTimeout))
                                        // even the job is done locally, ensure the result is never ever lost when in none-Optimistic mode
                                        if not this.Optimistic then
                                            do! redis.SetAsync(resultsKey + ":" + resultId, outMessage, 
                                                    Nullable(TimeSpan.FromMilliseconds(double resultTimeout)), When.Always, false)
                                                    |> Async.AwaitTask |> Async.Ignore
                                            do! redis.PublishAsync<string>(channelKey, resultId, false) |> Async.AwaitTask |> Async.Ignore
                                        localResultListeners.[resultId].Set() |> ignore
                                    else
                                        // alway store results in Redis if there is no local waiter, but fire and forget if in optimistic mode
                                        do!
                                            redis.SetAsync(resultsKey + ":" + resultId, outMessage, Nullable(TimeSpan.FromMilliseconds(double resultTimeout)), When.Always, this.Optimistic)
                                            |> Async.AwaitTask |> Async.Ignore
                                        do! redis.PublishAsync<string>(channelKey, resultId, this.Optimistic) |> Async.AwaitTask |> Async.Ignore
                                else
                                    // there is no result waiters, our job is to pass results directly to the second actor
                                    // in continuation and notify it that inbox is not empty
                                    
                                    for callerId in callerIds do
                                        //let callerInstance = ActorImpl<_,_>.ActorsRepo.[callerId]
                                        let callerInboxKey = "{" + callerId + "}" + ":inbox"
                                        let callerChannelKey = "{" + callerId + "}" + ":channel"
                                        let callerResultId = String.Join("|", resultId.Split('|').[1..]) // TODO check what Marc Gravel wrote about allocations and string splits
                                        let envelopeForCaller : Envelope<'TResult> =
                                            Envelope(outMessage,callerResultId,[||])
                                        do! redis.LPushAsync<Envelope<'TResult>>(callerInboxKey, envelopeForCaller, When.Always, false) 
                                            |> Async.AwaitTask |> Async.Ignore
                                        // empty notification for inbox
                                        do! redis.PublishAsync<string>(callerChannelKey, "", this.Optimistic) |> Async.AwaitTask |> Async.Ignore

                                        ()
                                    // CONTINUATION LOGIC - 
                                    // TODO 1. move to lua script
                                    // TODO 2. Add local optimization logic, probably will need to move some members to non-generic actor impl
                                    // otherwise <_,_> casts will fail because we do not know the final type of caller by its id
                                    // for each caller id we must pass current result to its inbox
                                    // get it instance

                                redis.HDel(pipelineKey, pipelineId, this.Optimistic) |> ignore
                            finally
                                Interlocked.Decrement(counter) |> ignore
                        }
                        |> Async.Start // do as many task as gates alow 
                }
            Async.Start(loop, cts.Token)
            started <- true

    let rec replayStalePipeline() =
        async {
            // TODO test that a message is returned to inbox
            // Using RPUSH so that task will be returned to the front of the queue
            let pipelineScript = 
                @"  local previousKey = KEYS[1]..':previousKeys'
                    local currentKey = KEYS[1]..':currentKeys'
                    local currentItems = redis.call('HKEYS', KEYS[1])
                    local res = 0
                    redis.call('DEL', currentKey)
                    if redis.call('HLEN', KEYS[1]) > 0 then
                       redis.call('SADD', currentKey, unpack(currentItems))
                       local intersect
                       if redis.call('SCARD', previousKey) > 0 then
                           intersect = redis.call('SINTER', previousKey, currentKey)
                           if #intersect > 0 then
                                local values = redis.call('HMGET', KEYS[1], unpack(intersect))
                                redis.call('RPUSH', KEYS[2], unpack(values))
                                redis.call('HDEL', KEYS[1], unpack(intersect))
                                res = #intersect
                           end
                       end
                    end
                    redis.call('DEL', previousKey)
                    if #currentItems > 0 then
                        redis.call('SADD', previousKey, unpack(currentItems))
                    end
                    return res
                "
            let expiry = Nullable<TimeSpan>(TimeSpan.FromMilliseconds(float garbageCollectionPeriod))
            let entered = redis.Set<string>(lockKey, "collecting garbage", 
                            expiry, When.NotExists, false)
            //Console.WriteLine("checking if entered: " + entered.ToString())
            let counts  =
                if entered then
                    let p =
                        if started then
                                redis.Eval(pipelineScript, [|redis.KeyNameSpace + ":" + pipelineKey; inboxKey|])
                        else ()
                    //Console.WriteLine("Collected pipelines: " + pipel.ToString() )
                    p
                else ()
            //do! Async.Sleep(garbageCollectionPeriod)
            do! Async.Sleep garbageCollectionPeriod
            return! replayStalePipeline()
            }

    do
        redis.Serializer <- JsonSerializer()
        checkGates() |> Async.Start
        replayStalePipeline() |> Async.Start
        if autoStart then start()

    static member LoadMonitor
        with get () = performanceMonitor
        and set monitor = performanceMonitor <- monitor
    static member Counter with get () = !counter
    static member val DefaultRedisConnectionString = "" with get, set
    static member ActorsRepo with get () = actors
    static member Instance<'Task, 'TResult>(definition:obj) : ActorImpl<'Task, 'TResult> = 
            let mutable key = ""
            // code duplication is OK here, otherwise will need interface, etc... and still type matching
            let actor =
                match definition with
                | x when isSubclassOfRawGeneric(typedefof<Actor<'Task, 'TResult>>, x.GetType()) -> // :? Actor<'Task, 'TResult> as taskDefinition -> 
                    let taskDefinition = x :?> Actor<'Task, 'TResult>
                    key <-  taskDefinition.GetKey()
                    if ActorImpl<_,_>.ActorsRepo.ContainsKey(key) then 
                            ActorImpl<_,_>.ActorsRepo.[key] :?> ActorImpl<'Task, 'TResult>
                    else
                        let conn = 
                            if String.IsNullOrWhiteSpace(taskDefinition.Redis) then
                                if String.IsNullOrWhiteSpace(ActorImpl<_,_>.DefaultRedisConnectionString) then
                                    raise (new ArgumentException("Redis connection string is not set"))
                                else
                                    ActorImpl<_,_>.DefaultRedisConnectionString
                            else taskDefinition.Redis
                        let comp (msg:Message<'Task> * string) : Async<Message<'TResult>> = taskDefinition.ExtendedComputation(msg)
                        ActorImpl(conn, key, comp, taskDefinition.ResultTimeout, taskDefinition.LowPriority, taskDefinition.AutoStart, taskDefinition.Optimistic)
                | x when isSubclassOfRawGeneric(typedefof<Ractor.FSharp.Actor<'Task, 'TResult>>, x.GetType()) -> //:? Ractor.FSharp.Actor<'Task, 'TResult> as asyncDefinition ->
                    let asyncDefinition = x :?> Ractor.FSharp.Actor<'Task, 'TResult>
                    key <-  asyncDefinition.GetKey()
                    if ActorImpl<_,_>.ActorsRepo.ContainsKey(key) then 
                            ActorImpl<_,_>.ActorsRepo.[key] :?> ActorImpl<'Task, 'TResult>
                    else
                        let conn = 
                            if String.IsNullOrWhiteSpace(asyncDefinition.Redis) then
                                if String.IsNullOrWhiteSpace(ActorImpl<_,_>.DefaultRedisConnectionString) then
                                    raise (new ArgumentException("Redis connection string is not set"))
                                else
                                    ActorImpl<_,_>.DefaultRedisConnectionString
                            else asyncDefinition.Redis
                        let comp (msg:Message<'Task> * string) : Async<Message<'TResult>> = asyncDefinition.ExtendedComputation(msg)
                        ActorImpl(conn, key, comp, asyncDefinition.ResultTimeout, asyncDefinition.LowPriority, asyncDefinition.AutoStart, asyncDefinition.Optimistic)
                | _ -> failwith "wrong definition type"
            ActorImpl<_,_>.ActorsRepo.[key] <- actor
            actor
    
    member internal this.Id = id
    member internal this.RedisConnectionString = redisConnectionString
    member internal this.Computation = computation
    member internal this.ResultTimeout = resultTimeout
    member internal this.LowPriority = lowPriority
    member internal this.Optimistic = optimistic

    member this.QueueLength = (int (redis.LLen(inboxKey))) + messageQueue.Count
    
    member this.Start() : unit = start()
    
    
    member this.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
        HostingEnvironment.UnregisterObject(this)
    

    /// <summary>
    /// Post message and get its assigned result Guid after the message was saved in Redis.
    /// </summary>
    /// <param name="message">Payload</param>
    member this.Post<'Task>(message : 'Task) : Guid = 
        this.PostAsync(message) |> Async.StartAsTask |> fun x -> x.Result // Async.RunSynchronously
    member this.TryPost<'Task>(message : 'Task, [<Out>] resultGuid : byref<Guid>) : bool = 
        let ok, guid = this.TryPostAsync(message) |> Async.RunSynchronously
        if ok then resultGuid <- guid
        ok

    /// <summary>
    /// Post message and get its assigned result Guid after the message was saved in Redis.
    /// </summary>
    /// <param name="message">Payload</param>
    member this.PostTask<'Task>(message : 'Task) : Task<Guid> = 
        this.PostAsync(message) |> Async.StartAsTask
    member this.TryPostTask<'Task>(message : 'Task) : Task<bool*Guid> = 
        this.TryPostAsync(message) |> Async.StartAsTask
    member this.PostAsync<'Task>(message : 'Task) : Async<Guid> = 
        async {
            let envelope = Envelope(Message(message, false, null), Guid.NewGuid().ToBase64String(), [||])
            let! str = this.Post(envelope)
            return str.GuidFromBase64String()
            }
    member this.TryPostAsync<'Task>(message : 'Task) : Async<bool*Guid> = 
        async {
            try
                let envelope = Envelope(Message(message, false, null), Guid.NewGuid().ToBase64String(), [||])
                let! result = this.Post(envelope)
                return true, result.GuidFromBase64String()
            with
            | _ -> return false, Unchecked.defaultof<Guid>
        }
    
    member internal this.Post<'Task>(envelope : Envelope<'Task>) : Async<string> = 
        let resultId = envelope.ResultId
        let remotePost() = 
            Console.WriteLine("Posted Redis message") 
            let res = 
                async {
                    do! redis.LPushAsync<Envelope<'Task>>(inboxKey, envelope, When.Always, this.Optimistic) 
                                |> Async.AwaitTask |> Async.Ignore
                    return resultId
                }
            // no resultId here because we notify recievers to process a message and they in turn will notify 
            // callers about results
            redis.Publish<string>(channelKey, "", this.Optimistic) |> ignore
            res
        let localPost() = 
            Debug.Print("Posted local message")
            localResultListeners.TryAdd(resultId, ManualResetEventSlim()) |> ignore 
            let pipelineId = Guid.NewGuid().ToBase64String()
            if not this.Optimistic then
                redis.HSet<Envelope<'Task>>(pipelineKey, pipelineId, envelope, When.Always, false) |> ignore
            messageQueue.Enqueue(envelope, pipelineId)
            messageWaiter.Set() |> ignore
            async {return resultId}
        match started with
        | true -> 
            async {
                let! opened = waitForOpenGates 0
                if opened then return! localPost()
                else return! remotePost()
            }    
        | _ -> remotePost()
          

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
        let resultId = resultGuid.ToBase64String()
        async {
            let! message = this.GetResultAsync(resultId)
            if message.HasError then return raise message.Error
            else return message.Value
        }

    /// <summary>
    /// Wait for a result and return it.
    /// </summary>
    /// <param name="resultId"></param>
    member internal this.GetResultAsync(resultId : string) : Async<Message<'TResult>> = 
        async{
            let! signaled = this.WaitResultAsync(resultId)
            // since we get there WaitResultAsync returned true and we must get the result
            let ok, result = this.TryGetResultImmediate(resultId)
            Debug.Assert(ok)
            return result
        }

    /// <summary>
    /// Wait until result is available. Always returns true or throws a timeout exception.
    /// </summary>
    member internal this.WaitResultAsync(resultId : string) : Async<bool> = 
        //Debug.Print("Getting: " + resultId)
        let cached = cache.Get(resultsKey + ":" + resultId)
        if cached <> null then
            async { return true }
        else 
            // in local case Post already added MRE and this line does nothing
            localResultListeners.TryAdd(resultId, ManualResetEventSlim()) |> ignore 
            let listener = localResultListeners.[resultId] 
            let rec awaitResult tryCount =
                async {
                    let retryInterval = if this.ResultTimeout > 0 then this.ResultTimeout / 3 else 5000 // arbitrary large number
                    let! listenerTask = Async.AwaitWaitHandle(listener.WaitHandle, retryInterval) |> Async.StartChild
                    let hasCachedId = cache.Get(resultsKey + ":id:" + resultId) <> null
                    let signaled = ref false
                    if not hasCachedId then
                        let! signal = listenerTask
                        signaled := signal // false if intermediate timeout that is needed just in case Redis PubSub is not 100% reliable
                    else
                        signaled := true
                    if !signaled then // if signaled then result definitely exists
                        return true
                    else // opportunistic retry "what if we lost a message for some reason"
                        let cachedResult = cache.Get(resultsKey + ":" + resultId)
                        let result =
                            if cachedResult <> null then true
                            else
                                // TODO HEXISTS!
                                 (redis.Exists(resultsKey + ":" + resultId)) // not
                        if not result then
                            if tryCount > 2 then Debug.Fail("Cannot get result for: " + resultId)
                            // in release mode we will get timeout from the outer async
                            return! awaitResult (tryCount + 1)
                        else
                            return true
                }
            async { let! t = Async.StartChild(awaitResult 0, this.ResultTimeout)
                    return! t }

    /// <summary>
    /// Check if a result is available and return it.
    /// </summary>
    member internal this.TryGetResultImmediate(resultId : string, [<Out>] result : byref<Message<'TResult>>) : bool = 
        let cachedResult = cache.Get(resultsKey + ":" + resultId)
        let result' : Message<'TResult> =
            if cachedResult <> null then unbox cachedResult
            else redis.Get<Message<'TResult>>(resultsKey + ":" + resultId)
        if Object.Equals(result', null) then
            false
        else
            result <- result'
            true
        
    /// <summary>
    /// Check if a result is likely awailable and return it.
    /// </summary>
    member internal this.TryGetResultIfItDefinitelyExists(resultId : string, [<Out>] result : byref<Message<'TResult>>) : bool = 
        let hasCachedId = cache.Get(resultsKey + ":id:" + resultId) <> null
        //false doesn't mean that result doesn't exist (it doesn't in 99.9..% cases), but true 100% means that we have a result
        // in most cases (depends on Redis's PubSub) this variable is right
        if hasCachedId then 
            let cachedResult = cache.Get(resultsKey + ":" + resultId)
            let result' : Message<'TResult> =
                if cachedResult <> null then unbox cachedResult
                else redis.Get<Message<'TResult>>(resultsKey + ":" + resultId)
            if Object.Equals(result', null) then
                false
            else
                result <- result'
                true
        else false


    member this.TryGetResultAsync(resultGuid : Guid) : Async<bool*'TResult> = 
        async {
            try
                let! result = this.GetResultAsync(resultGuid)
                return true, result
            with
            | _ -> return false, Unchecked.defaultof<'TResult>
        }

    member this.PostAndGetResult(message : 'Task) : 'TResult = 
        this.PostAndGetResultAsync(message) |> Async.RunSynchronously
    member this.TryPostAndGetResult(message : 'Task, [<Out>] result : byref<'TResult>) : bool = 
        try
            result <- this.PostAndGetResultAsync(message) |> Async.RunSynchronously
            true
        with
        | _ -> 
            result <- Unchecked.defaultof<'TResult>
            false
    member this.PostAndGetResultTask(message : 'Task) : Task<'TResult> = 
        this.PostAndGetResultAsync(message) |> Async.StartAsTask
    member this.TryPostAndGetResultTask(message : 'Task) : Task<bool * 'TResult> = 
        this.TryPostAndGetResultAsync(message) |> Async.StartAsTask

    member this.PostAndGetResultAsync(message : 'Task) : Async<'TResult> = 
        async {
            let envelope = Envelope(Message(message, false, null), Guid.NewGuid().ToBase64String(), [||])
            let! message = this.PostAndGetResult(envelope)
            if message.HasError then return raise message.Error
            else return message.Value
        }

    member this.TryPostAndGetResultAsync(message : 'Task) : Async<bool * 'TResult> = 
        async {
            try
                let! result = this.PostAndGetResultAsync(message)
                return true, result
            with
            | _ -> return false, Unchecked.defaultof<'TResult>
        }
        
    member internal this.PostAndGetResult(envelope : Envelope<'Task>) : Async<Message<'TResult>> = 
        let inMessage, resultId = envelope.Message, envelope.ResultId
        let standardCall() = async {
                do! this.Post(envelope) |> Async.Ignore
                return! this.GetResultAsync(resultId)
            }
        let shortcutCall() = // avoid most of the async machinery
            async {
                try 
                    Interlocked.Increment(counter) |> ignore
                    // PaGR used only with outside callers (not continuations), that means:
                    // 1. do not store result in Redis since noone knows the id to retrieve it other than the current 
                    // method call - if it dies then the stored result is garbage.
                    // 2. if we store in pipeline and die during execution, the message will be put back into inbox,
                    // processed, but who will get the result? If processing is without side-effects then we 
                    // do not care.
                    // Therefore we do not need to use pipeline or result storage with the local shortcutCall.
                    // With remote call, a caller must be alive and keep reference (resultId) and wait for that result
                    // While the caller is alive, we guarantee it will get its result. If caller dies, the result turns into garbage.
                    let! child = Async.StartChild(computation (inMessage, resultId), this.ResultTimeout)
                    let! outMessage = child
                    return outMessage
                finally
                    Interlocked.Decrement(counter) |> ignore
            }
        // do shortcut calls only in optimistic mode
        match started && this.Optimistic with
        | true -> 
            async {
                let! opened = waitForOpenGates 0
                if opened then return! shortcutCall()
                else return! standardCall()
            }
        | _ -> standardCall()

    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            cts.Dispose()
    interface IRegisteredObject with
        member x.Stop(immediate : bool) = x.Stop()


// TODO copy docs comments from Actors
[<Extension>]
type ActorExtension() =
    [<Extension>]
    static member Start(this : Actor<'Task, 'TResult>) : unit = 
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.Start()
    [<Extension>]
    static member Stop(this : Actor<'Task, 'TResult>) : unit = 
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.Stop()
    [<Extension>]
    static member GetTotalCount(this : Actor<'Task, 'TResult>) = 
        ActorImpl<'Task, 'TResult>.Counter

    [<Extension>]
    static member Post<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task) : Guid =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.Post(message)
    [<Extension>]
    static member PostAsync<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task) : Task<Guid> =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.PostTask(message)
    [<Extension>]
    static member TryPost<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task, [<Out>] resultGuid : byref<Guid>) =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.TryPost(message, &resultGuid)
    [<Extension>]
    static member TryPostAsync<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task) =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.TryPostTask(message)
    
    [<Extension>]
    static member GetResult(this : Actor<'Task, 'TResult>, resultGuid : Guid) : 'TResult = 
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.GetResult(resultGuid)
    [<Extension>]
    static member TryGetResult(this : Actor<'Task, 'TResult>, resultGuid : Guid, [<Out>] result : byref<'TResult>) : bool = 
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.TryGetResult(resultGuid, &result)
    [<Extension>]
    static member GetResultAsync(this : Actor<'Task, 'TResult>, resultGuid : Guid) : Task<'TResult> = 
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.GetResultTask(resultGuid)
    [<Extension>]
    static member TryGetResultAsync(this : Actor<'Task, 'TResult>, resultGuid : Guid) : Task<bool*'TResult> = 
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.TryGetResultTask(resultGuid)

    [<Extension>]
    static member PostAndGetResult<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task) : 'TResult =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.PostAndGetResult(message)
    [<Extension>]
    static member PostAndGetResultAsync<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task) : Task<'TResult> =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.PostAndGetResultTask(message)
    [<Extension>]
    static member TryPostAndGetResult<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task, [<Out>] result : byref<'TResult>) : bool =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.TryPostAndGetResult(message, &result)
    [<Extension>]
    static member TryPostAndGetResultAsync<'Task, 'TResult>(this : Actor<'Task, 'TResult>, message : 'Task) : Task<bool*'TResult> =
        let actor = ActorImpl<'Task, 'TResult>.Instance(this)
        actor.TryPostAndGetResultTask(message)
    [<Extension>]
    static member ContinueWith<'Task, 'TResult1, 'TResult2>
        (this : Actor<'Task, 'TResult1>, 
            continuation : Actor<'TResult1, 'TResult2>) : Actor<'Task, 'TResult2> =
                let actor1 = ActorImpl<'Task, 'TResult1>.Instance(this)
                let actor2 = ActorImpl<'TResult1, 'TResult2>.Instance(continuation)
                let key = "(" + this.GetKey() + "->-" + continuation.GetKey() + ")"
                let computation : Message<'Task> * string -> Async<Message<'TResult2>> = 
                    fun (inMessage, resultId) -> 
                        async {
                            // 1. check if we have the final result. that could *very rarely* happen if 
                            // we resume work from dead continuator while actor1 and actor2 are alive
                            // (it takes a local cache lookup - very cheap)
                            let secondIsDone, r2 = actor2.TryGetResultIfItDefinitelyExists(resultId)
                            if secondIsDone then // extremely unusual
                                return r2
                            else
                                // 2. check if we already have a result from the first, 
                                // and if so do not post to both the first and the second actor
                                // because the first is done and the second *MUST* have that result in its inbox
                                // (when the first actor was called with non-empty caller id, the result of the first
                                // call is "atomically" added to the second actor inbox. (atomically means 
                                // that if actor.GetResultAsync() returned then the intermediate result
                                // is already in cActor inbox).
                                // (Such a situation when firstIsDone is more likely with continuations that could be chained mutiple times.)
                                let firstIsDone, _ = actor1.TryGetResultIfItDefinitelyExists(key + "|" + resultId)
                                if firstIsDone then // extremely unusual
                                    // The first result must be already in the second inbox/pipeline
                                    // Do not need to get the intermediate result here, do nothing
                                    ()
                                else // normal case
                                    // here the task message is sent to the first actor and 
                                    // it will arrive to the results list and stay there
                                    // before the results timeout
                                    let envelope : Envelope<'Task> = Envelope(inMessage, key + "|" + resultId, [| actor2.Id |])
                                    do! actor1.Post(envelope) |> Async.Ignore
                                    // do not wait for intermediate result
                                    ()
                                // wait for second actor
                                let! r2 = actor2.GetResultAsync(resultId)
                                return r2
                        }
            
                let result = 
                    { new Actor<'Task, 'TResult2>() with
                           override __.Redis with get() = actor1.RedisConnectionString
                           override __.GetKey() = key
                           override __.ResultTimeout with get() = this.ResultTimeout + continuation.ResultTimeout
                           //instead of [override __.Computation(input) = ...] assign internal computation directly
                    }
                // overwrite the default computation that ignores resultId with the proper one
                result.ExtendedComputation <- computation
                result


namespace Ractor.FSharp
[<AutoOpenAttribute>]
module FSharpExtensions =
    open System
    open Ractor
    open Ractor.FSharp
    open System.Threading
    open System.Runtime.CompilerServices
    open System.Runtime.InteropServices
    open System.Diagnostics

    type Actor<'Task, 'TResult> with
        member this.Start() : unit = 
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.Start()
        member this.Stop() : unit = 
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.Stop()
        
        member this.GetTotalCount() = 
            ActorImpl<'Task, 'TResult>.Counter

        
        member this.Post<'Task, 'TResult>( message : 'Task) : Guid =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.Post(message)
        
        member this.PostAsync<'Task, 'TResult>( message : 'Task) : Async<Guid> =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.PostAsync(message)
        
        member this.TryPost<'Task, 'TResult>( message : 'Task, [<Out>] resultGuid : byref<Guid>) =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.TryPost(message, &resultGuid)
        
        member this.TryPostAsync<'Task, 'TResult>( message : 'Task) =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.TryPostAsync(message)
    
        
        member this.GetResult( resultGuid : Guid) : 'TResult = 
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.GetResult(resultGuid)
        
        member this.TryGetResult( resultGuid : Guid, [<Out>] result : byref<'TResult>) : bool = 
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.TryGetResult(resultGuid, &result)
        
        member this.GetResultAsync( resultGuid : Guid) : Async<'TResult> = 
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.GetResultAsync(resultGuid)
        
        member this.TryGetResultAsync( resultGuid : Guid) : Async<bool*'TResult> = 
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.TryGetResultAsync(resultGuid)

        
        member this.PostAndGetResult<'Task, 'TResult>( message : 'Task) : 'TResult =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.PostAndGetResult(message)
        
        member this.PostAndGetResultAsync<'Task, 'TResult>( message : 'Task) : Async<'TResult> =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.PostAndGetResultAsync(message)
        
        member this.TryPostAndGetResult<'Task, 'TResult>( message : 'Task, [<Out>] result : byref<'TResult>) : bool =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.TryPostAndGetResult(message, &result)
        
        member this.TryPostAndGetResultAsync<'Task, 'TResult>( message : 'Task) : Async<bool*'TResult> =
            let actor = ActorImpl<'Task, 'TResult>.Instance(this)
            actor.TryPostAndGetResultAsync(message)
    
        
        //#region This is exact copy of ContinueWith on Ractor.Actor: do not edit this, copy from above
        member this.ContinueWith<'Task, 'TResult1, 'TResult2>
            (continuation : Actor<'TResult1, 'TResult2>) : Actor<'Task, 'TResult2> =
                let actor1 = ActorImpl<'Task, 'TResult1>.Instance(this)
                let actor2 = ActorImpl<'TResult1, 'TResult2>.Instance(continuation)
                let key = "(" + this.GetKey() + "->-" + continuation.GetKey() + ")"
                let computation : Message<'Task> * string -> Async<Message<'TResult2>> = 
                    // this resultId is passed from continuator call and it is in ShortGuid format
                    fun (inMessage, resultId) -> 
                        async {
                            // 1. check if we have the final result. that could *very rarely* happen if 
                            // we resume work from dead continuator while actor1 and actor2 are alive
                            // (it takes a local cache lookup - very cheap)
                            let secondIsDone, r2 = actor2.TryGetResultIfItDefinitelyExists(resultId)
                            if secondIsDone then // extremely unusual
                                return r2
                            else
                                // 2. check if we already have a result from the first, 
                                // and if so do not post to both the first and the second actor
                                // because the first is done and the second *MUST* have that result in its inbox
                                // (when the first actor was called with non-empty caller id, the result of the first
                                // call is "atomically" added to the second actor inbox. (atomically means 
                                // that if actor.GetResultAsync() returned then the intermediate result
                                // is already in cActor inbox).
                                // (Such a situation when firstIsDone is more likely with continuations that could be chained mutiple times.)
                                let firstIsDone, _ = actor1.TryGetResultIfItDefinitelyExists(key + "|" + resultId)
                                if firstIsDone then // extremely unusual
                                    // The first result must be already in the second inbox/pipeline
                                    // Do not need to get the intermediate result here, do nothing
                                    ()
                                else // normal case
                                    // here the task message is sent to the first actor and 
                                    // it will arrive to the results list and stay there
                                    // before the results timeout
                                    let envelope : Envelope<'Task> = Envelope(inMessage, key + "|" + resultId, [| actor2.Id |])
                                    do! actor1.Post(envelope) |> Async.Ignore
                                    // do not wait for intermediate result
                                    ()
                                // wait for second actor
                                let! r2 = actor2.GetResultAsync(resultId)
                                return r2
                        }
            
                let result = 
                    { new Actor<'Task, 'TResult2>() with
                           override __.Redis with get() = actor1.RedisConnectionString
                           override __.GetKey() = key
                           override __.ResultTimeout with get() = this.ResultTimeout + continuation.ResultTimeout
                           //instead of [override __.Computation(input) = ...] assign internal computation directly
                    }
                // overwrite the default computation that ignores resultId with the proper one
                result.ExtendedComputation <- computation
                result
        //#endregion


