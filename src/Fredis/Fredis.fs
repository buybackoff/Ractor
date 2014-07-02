namespace Fredis

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Fredis

// TODO System messages
// TODO tags for conditional execution - start only if some tags in environment are set
// e.g. AWS tags for crawlers, app servers, front ends - the deployment code should be 
// identical everywhere but the actor behavior should depend on the tags in the environment
// TODO test high CPU load and many (1000+) IO-like actors

type Fredis(redisConnectionString : string) = 
    let redis = Redis(redisConnectionString, "Fredis")
       
    static let dbs = Dictionary<string, IPocoPersistor>()
    static let blobs = Dictionary<string, IBlobPersistor>()
    static let redises = Dictionary<string, Redis>()  

    do
        redis.Serializer <- PicklerBinarySerializer()

        if String.IsNullOrEmpty(ActorImpl<_,_>.DefaultRedisConnectionString) then
            ActorImpl<_,_>.DefaultRedisConnectionString <- redisConnectionString

    static let mutable logger = 
#if DEBUG
        Logging.Console
#else
        Logging.Silent
#endif

    static member Logger 
        with get () = logger
        and set newLogger = 
            ActorBase.Logger <- newLogger
            logger <- newLogger

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

// erased by compiler, convenient to use from F#
type F = Fredis


// TODO uncomment and rethink operators
//[<AutoOpen>]
//module Operators = 
//    let (<--) (id : string) (msg : 'Task) : unit = Fredis.GetActor<'Task, unit>(id).Post(msg)
//    let (-->) (msg : 'Task) (id : string) : unit = Fredis.GetActor<'Task, unit>(id).Post(msg)
//    let (<-*) (id : string) (msg : 'Task) : Async<'TResult> = 
//        Fredis.GetActor<'Task, 'TResult>(id).PostAndGetResult(msg, Timeout.Infinite)
//    let ( *-> ) (msg : 'Task) (id : string) : Async<'TResult> = 
//        Fredis.GetActor<'Task, 'TResult>(id).PostAndGetResult(msg, Timeout.Infinite)
    



// TODO do we really need 1-to-many and many-to-1 continuations as a separate methods,
// or it is good enough to call one actor from another... 
// we need to store a reference (Guid) as a part of message to continuator so that
// it could retry on power shutdown etc. Same issue as with 1-to-1.
// 
// ContinueWith
// ->>- 1-1
// ->>= 1-many
// =>>- many - 1
// should filter unit if that is possible