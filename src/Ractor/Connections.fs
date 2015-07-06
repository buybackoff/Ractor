namespace Ractor

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Ractor

// TODO move to the persistence project

// TODO System messages
// TODO tags for conditional execution - start only if some tags in environment are set
// e.g. AWS tags for crawlers, app servers, front ends - the deployment code should be 
// identical everywhere but the actor behavior should depend on the tags in the environment
// TODO test high CPU load and many (1000+) IO-like actors

type Connections(redisConnectionString : string) = 
    //let redis = Redis(redisConnectionString, "R")
       
    static let dbs = Dictionary<string, IPocoPersistor>()
    static let blobs = Dictionary<string, IBlobPersistor>()
    static let redises = Dictionary<string, Redis>()  

//    do
//        redis.Serializer <- JsonSerializer()
//        if String.IsNullOrEmpty(ActorImpl<_,_>.DefaultRedisConnectionString) then
//            ActorImpl<_,_>.DefaultRedisConnectionString <- redisConnectionString

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

    static member RegisterDB(persistor : IPocoPersistor) = Connections.RegisterDB(persistor, "")
    
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
    
    static member RegisterBlobStorage(persistor : IBlobPersistor) = Connections.RegisterBlobStorage(persistor, "")
    static member GetBlobStorage(id : string) = 
        let id = id.ToLowerInvariant() 
        blobs.[id]
    static member GetBlobStorage() = blobs.[""]
    
    static member RegisterRedis(redis : Redis, id : string) = 
        let id = id.ToLowerInvariant() 
        if redises.ContainsKey(id) then raise (InvalidOperationException("Redis with the same id already exists: " + id))
        redises.Add(id, redis)
        redis
    
    static member RegisterRedis(redis : Redis) = Connections.RegisterRedis(redis, "")
    static member GetRedis(id : string) = 
        let id = id.ToLowerInvariant() 
        redises.[id]
    static member GetRedis() = redises.[""]
    static  member internal GetOrCreateRedis(connectionString : string, ns : string) = 
        let key = (ns + ":" + connectionString).ToLowerInvariant()
        if redises.ContainsKey(key) then
            redises.[key]
        else
            let newRedis = Redis(connectionString, ns)
            redises.[key] <- newRedis
            newRedis


