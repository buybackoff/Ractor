namespace Fredis

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Fredis

// TODO System messages & tags
// TODO test high CPU load and many (1000+) IO-like actors
type Fredis(connectionString : string) = 
    let redis = Redis(connectionString, "Fredis")
    do redis.Serializer <- Serialisers.Pickler
    // It could be possible to have several instances of Fredis, via this repo we
    // could access them by name. We just require unique names accross all Fredis instances
    static let actors = Dictionary<string, obj>()
    static let dbs = Dictionary<string, IPocoPersistor>()
    static let blobs = Dictionary<string, IBlobPersistor>()
    static let redises = Dictionary<string, Redis>()
    
    // We intentinally limit Actor creation to instance method of Fredis, not a static method
    // plus connection string. There is a way to make actors on different redis dbs, but only via
    // different fredis instances.
    member internal this.CreateActor<'Tin, 'Tout>(id : string, computation : 'Tin -> Async<'Tout>) = 
        if actors.ContainsKey(id) then raise (InvalidOperationException("Agent with the same id already exists: " + id))
        let actor = new Actor<'Tin, 'Tout>(redis, id, computation)
        actors.Add(id, actor)
        actor
    
    member this.CreateActor<'Tin, 'Tout>(id : string, computation : Func<'Tin, Task<'Tout>>) = 
        let comp msg = computation.Invoke(msg) |> Async.AwaitTask
        this.CreateActor(id, comp)
    
//    member this.CreateActor<'T>(id : string, computation : Action<'T>) = 
//        let comp msg = async { computation.Invoke(msg) }
//        this.CreateActor(id, comp)
    
    static member GetActor<'Tin, 'Tout>(id : string) : Actor<'Tin, 'Tout> = unbox actors.[id] //:?> Actor<'Tin, 'Tout>
    static member RegisterDB(persistor : IPocoPersistor) = Fredis.RegisterDB(persistor, "")
    
    static member RegisterDB(persistor : IPocoPersistor, id : string) = 
        if dbs.ContainsKey(id) then raise (InvalidOperationException("DB with the same id already exists: " + id))
        dbs.Add(id, persistor)
        persistor
    
    static member GetDB(id : string) = dbs.[id]
    static member GetDB() = dbs.[""]
    
    static member RegisterBlobStorage(persistor : IBlobPersistor, id : string) = 
        if dbs.ContainsKey(id) then 
            raise (InvalidOperationException("Blob Storage with the same id already exists: " + id))
        blobs.Add(id, persistor)
        persistor
    
    static member RegisterBlobStorage(persistor : IBlobPersistor) = Fredis.RegisterBlobStorage(persistor, "")
    static member GetBlobStorage(id : string) = blobs.[id]
    static member GetBlobStorage() = blobs.[""]
    
    static member RegisterRedis(redis : Redis, id : string) = 
        if dbs.ContainsKey(id) then raise (InvalidOperationException("Redis with the same id already exists: " + id))
        redises.Add(id, redis)
        redis
    
    static member RegisterRedis(redis : Redis) = Fredis.RegisterRedis(redis, "")
    static member GetRedis(id : string) = redises.[id]
    static member GetRedis() = redises.[""]




[<AutoOpen>]
module Operators = 
    let (<--) (id : string) (msg : 'Tin) : unit = Fredis.GetActor<'Tin, unit>(id).Post(msg)
    let (-->) (msg : 'Tin) (id : string) : unit = Fredis.GetActor<'Tin, unit>(id).Post(msg)
    let (<-*) (id : string) (msg : 'Tin) : Async<'Tout> = 
        Fredis.GetActor<'Tin, 'Tout>(id).PostAndReply(msg, false, Timeout.Infinite)
    let ( *-> ) (msg : 'Tin) (id : string) : Async<'Tout> = 
        Fredis.GetActor<'Tin, 'Tout>(id).PostAndReply(msg, false, Timeout.Infinite)
    let (->>-) (parent : string) (child : string) = Fredis.GetActor(parent).Link(Fredis.GetActor(child))
    let (-<<-) (child : string) (parent : string) = Fredis.GetActor(parent).Link(Fredis.GetActor(child))
    
    let (->>=) (parent : string) (children : seq<string>) = 
        let parent = Fredis.GetActor(parent)
        Seq.iter (fun child -> parent.Link(Fredis.GetActor(child)) |> ignore) children
        parent
