namespace Fredis

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Fredis

// TODO System messages & tags
// TODO test high CPU load and many (1000+) IO-like actors

// TODO create IActorDefinition and this.RegisterActorsFrom(TypeFromAssembly/assembly name/namespace)
// which will find all actors implementation via reflection
// that way we could write actors in a separate place and then use them via GetActor<>("name")
// also should specify redis connection string as part of actor definition and check if
// null or empty - if not set then use default, that way we could change one line in
// actor definition and it will run on a separate Redis instance
// TODO case-insensitive names

type Fredis(connectionString : string) = 
    let redis = Redis(connectionString, "Fredis")
    
    // It could be possible to have several instances of Fredis, via this repo we
    // could access actors from any one by name. We just require unique names accross all Fredis instances

    
    static let dbs = Dictionary<string, IPocoPersistor>()
    static let blobs = Dictionary<string, IBlobPersistor>()
    static let redises = Dictionary<string, Redis>()

    static let semaphor = new SemaphoreSlim(Environment.ProcessorCount * 64000) // TODO what is the best limit?
    static let counter = ref 0
    static let lowPriorityGate = new ManualResetEventSlim(true)
    static let mutable performanceMonitor = 
        { new IFredisPerformanceMonitor with
              member x.AllowLowPriorityActors() = !counter < Environment.ProcessorCount * 64 
              member x.FrequencySeconds = 60 }
    let rec checkLowPriorityGate() =
        async {
            if performanceMonitor.AllowLowPriorityActors() then lowPriorityGate.Set()
            else lowPriorityGate.Reset()
            do! Async.Sleep(performanceMonitor.FrequencySeconds * 1000)
            return! checkLowPriorityGate()
        }

    do
        redis.Serializer <- Serialisers.Pickler
        checkLowPriorityGate() |> Async.Start

    // could set to custom implementation or could forget about it with default dummy one
    static member FredisPerformanceMonitor 
        with get () = performanceMonitor
        and set monitor = performanceMonitor <- monitor

    member this.CreateActor<'Task, 'TResult>(id : string, computation : 'Task -> Async<'TResult>) = 
        this.CreateActor<'Task, 'TResult>(id, computation, false)
    member this.CreateActor<'Task, 'TResult>(id : string, computation : 'Task -> Async<'TResult>, lowPriority) = 
        this.CreateActor(id, computation, Timeout.Infinite, lowPriority)
    // We intentinally limit Actor creation to instance method of Fredis, not a static method
    // plus connection string. There is a way to make actors on different redis dbs, but only via
    // different fredis instances.
    member this.CreateActor<'Task, 'TResult>(id : string, computation : 'Task -> Async<'TResult>, computationTimeout, lowPriority) = 
        let id = id.ToLowerInvariant()
        if Actor<_,_>.ActorsRepo.ContainsKey(id) then raise (InvalidOperationException("Agent with the same id already exists: " + id))
        let comp : 'Task * string -> Async<'TResult> = (fun message -> computation(fst message))
        let actor = new Actor<'Task, 'TResult>(connectionString, id, comp, computationTimeout, lowPriority)
        actor.semaphor <- semaphor
        actor.counter <- counter
        actor.lowPriorityGate <- lowPriorityGate
        Actor<_,_>.ActorsRepo.[id] <- actor // TODO move inside Actor constructor
        actor

    // for testing
    member this.CloneActor<'Task, 'TResult>(id : string)=
        let actor = Fredis.GetActor<'Task, 'TResult>(id)
        let clone = new Actor<'Task, 'TResult>(actor.RedisConnectionString, actor.Id, actor.Computation, 
                        actor.ComputationTimeout, actor.LowPriority)
        clone.semaphor <- semaphor
        clone.counter <- counter
        clone.lowPriorityGate <- lowPriorityGate
        clone

    

    
    member this.CreateActor<'Task, 'TResult>(id : string, computation : Func<'Task, Task<'TResult>>, lowPriority) = 
        let comp msg = computation.Invoke(msg) |> Async.AwaitTask
        this.CreateActor(id, comp, lowPriority)
    
    member this.CreateActor<'Task, 'TResult>(id : string, computation : Func<'Task, Task<'TResult>>) = 
        let comp msg = computation.Invoke(msg) |> Async.AwaitTask
        this.CreateActor(id, comp)

    member this.CreateActor<'T>(id : string, computation : Action<'T>, lowPriority) = 
        let comp msg = async { computation.Invoke(msg) }
        this.CreateActor(id, comp, lowPriority)
    
    member this.CreateActor<'T>(id : string, computation : Action<'T>) = 
        let comp msg = async { computation.Invoke(msg) }
        this.CreateActor(id, comp)


    static member GetActor<'Task, 'TResult>(id : string) : Actor<'Task, 'TResult> = 
        let id = id.ToLowerInvariant()
        unbox Actor<_,_>.ActorsRepo.[id]

    static member GetActor<'Task>(id : string) : Actor<'Task, unit> = 
        let id = id.ToLowerInvariant()
        unbox Actor<_,unit>.ActorsRepo.[id]

    static member RegisterDB(persistor : IPocoPersistor) = Fredis.RegisterDB(persistor, "")
    
    static member RegisterDB(persistor : IPocoPersistor, id : string) = 
        let id = id.ToLowerInvariant()
        if dbs.ContainsKey(id) then raise (InvalidOperationException("DB with the same id already exists: " + id))
        dbs.Add(id, persistor)
        persistor
    
    static member GetDB(id : string) = 
        let id = id.ToLowerInvariant()
        dbs.[id]
    static member GetDB() = dbs.[""]
    
    static member RegisterBlobStorage(persistor : IBlobPersistor, id : string) =
        let id = id.ToLowerInvariant() 
        if blobs.ContainsKey(id) then 
            raise (InvalidOperationException("Blob Storage with the same id already exists: " + id))
        blobs.Add(id, persistor)
        persistor
    
    static member RegisterBlobStorage(persistor : IBlobPersistor) = Fredis.RegisterBlobStorage(persistor, "")
    static member GetBlobStorage(id : string) = 
        let id = id.ToLowerInvariant() 
        blobs.[id]
    static member GetBlobStorage() = blobs.[""]
    
    static member RegisterRedis(redis : Redis, id : string) = 
        let id = id.ToLowerInvariant() 
        if redises.ContainsKey(id) then raise (InvalidOperationException("Redis with the same id already exists: " + id))
        redises.Add(id, redis)
        redis
    
    static member RegisterRedis(redis : Redis) = Fredis.RegisterRedis(redis, "")
    static member GetRedis(id : string) = 
        let id = id.ToLowerInvariant() 
        redises.[id]
    static member GetRedis() = redises.[""]

[<AutoOpen>]
module Operators = 
    let (<--) (id : string) (msg : 'Task) : unit = Fredis.GetActor<'Task, unit>(id).Post(msg)
    let (-->) (msg : 'Task) (id : string) : unit = Fredis.GetActor<'Task, unit>(id).Post(msg)
    let (<-*) (id : string) (msg : 'Task) : Async<'TResult> = 
        Fredis.GetActor<'Task, 'TResult>(id).PostAndGetResult(msg, Timeout.Infinite)
    let ( *-> ) (msg : 'Task) (id : string) : Async<'TResult> = 
        Fredis.GetActor<'Task, 'TResult>(id).PostAndGetResult(msg, Timeout.Infinite)
    



// TODO
// ContinueWith
// ->>- 1-1
// ->>= 1-many
// =>>- many - 1
// should filter unit if that is possible