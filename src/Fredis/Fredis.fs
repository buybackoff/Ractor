namespace Fredis

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Fredis

// TODO System messages & tags
// TODO test high CPU load and many (1000+) IO-like actors
// TODO on Fredis creatin check if static Actor.DefaultRedisConnection is set - if not, set it to Fredis's one

type Fredis(connectionString : string) = 
    let redis = Redis(connectionString, "Fredis")
    
    // It could be possible to have several instances of Fredis, via this repo we
    // could access actors from any one by name. We just require unique names accross all Fredis instances
    
    static let dbs = Dictionary<string, IPocoPersistor>()
    static let blobs = Dictionary<string, IBlobPersistor>()
    static let redises = Dictionary<string, Redis>()  

    do
        redis.Serializer <- Serialisers.Pickler
        // TODO if redis with id "" is not set yet set it to the first one created
        // TODO on Fredis creatin check if static Actor.DefaultRedisConnection is set - if not, set it to Fredis's one

    // for testing
    member internal this.CloneActor<'Task, 'TResult>(def : Actor<'Task, 'TResult>)=
        let actor = ActorImpl.Instance(def)
        let clone = new ActorImpl<_,_>(actor.RedisConnectionString, actor.Id, actor.Computation, 
                        actor.ResultTimeout, actor.LowPriority, actor.Optimistic)
        //clone.semaphor <- semaphor
        clone

    member internal this.CloneActor<'Task, 'TResult>(def : Fredis.FSharp.Actor<'Task, 'TResult>)=
        let actor = ActorImpl.Instance(def)
        let clone = new ActorImpl<_,_>(actor.RedisConnectionString, actor.Id, actor.Computation, 
                        actor.ResultTimeout, actor.LowPriority, actor.Optimistic)
        //clone.semaphor <- semaphor
        clone


//    static member Actor<'Task, 'TResult>(definition: Actor<'Task, 'TResult>) = 
//        ActorImpl<_,_>.Instance(definition)


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


type F = Fredis

//[<AutoOpen>]
//module Operators = 
//    let (<--) (id : string) (msg : 'Task) : unit = Fredis.GetActor<'Task, unit>(id).Post(msg)
//    let (-->) (msg : 'Task) (id : string) : unit = Fredis.GetActor<'Task, unit>(id).Post(msg)
//    let (<-*) (id : string) (msg : 'Task) : Async<'TResult> = 
//        Fredis.GetActor<'Task, 'TResult>(id).PostAndGetResult(msg, Timeout.Infinite)
//    let ( *-> ) (msg : 'Task) (id : string) : Async<'TResult> = 
//        Fredis.GetActor<'Task, 'TResult>(id).PostAndGetResult(msg, Timeout.Infinite)
    



// TODO
// ContinueWith
// ->>- 1-1
// ->>= 1-many
// =>>- many - 1
// should filter unit if that is possible