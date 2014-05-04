namespace Fredis

open System
open System.Collections.Generic
open System.Threading.Tasks

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
    member this.CreateActor<'Tin, 'Tout>(id : string, computation : 'Tin -> Async<'Tout>) = 
        if actors.ContainsKey(id) then raise (InvalidOperationException("Agent with the same id already exists: " + id))
        let actor = new Actor<'Tin, 'Tout>(redis, id, computation)
        actors.Add(id, actor)
        actor
    
    member this.CreateActor<'Tin, 'Tout>(id : string, computation : Func<'Tin, Task<'Tout>>) = 
        let comp msg = async { return computation.Invoke(msg) |> Async.AwaitTask }
        this.CreateActor(id, comp)
    
    
    static member GetActor<'Tin, 'Tout>(id : string) : Actor<'Tin, 'Tout> = 
        actors.[id] :?> Actor<'Tin, 'Tout>

    static member RegisterDB(persistor:IPocoPersistor, ?id:string) =
        let id = defaultArg id ""
        if dbs.ContainsKey(id) then raise (InvalidOperationException("DB with the same id already exists: " + id))
        dbs.Add(id, persistor)
        persistor


    static member GetDB(?id : string) = 
            let id = defaultArg id ""
            dbs.[id]

    static member RegisterBlobStorage(persistor:IBlobPersistor, ?id:string) =
        let id = defaultArg id ""
        if dbs.ContainsKey(id) then raise (InvalidOperationException("Blob Storage with the same id already exists: " + id))
        blobs.Add(id, persistor)
        persistor


    static member GetBlobStorage(?id : string) = 
            let id = defaultArg id ""
            blobs.[id]


    static member RegisterRedis(redis:Redis, ?id:string) =
        let id = defaultArg id ""
        if dbs.ContainsKey(id) then raise (InvalidOperationException("Redis with the same id already exists: " + id))
        redises.Add(id, redis)
        redis

    static member GetRedis(?id : string) = 
            let id = defaultArg id ""
            redises.[id]



[<AutoOpen>]
module Operators = 
    let (<--) (id : string) (msg : 'Tin) : unit = Fredis.GetActor<'Tin, unit>(id).Post(msg)
    let (-->) (msg : 'Tin) (id : string)  : unit = Fredis.GetActor<'Tin, unit>(id).Post(msg)

    let (<-*) (id : string) (msg : 'Tin) : Async<'Tout> = Fredis.GetActor<'Tin, 'Tout>(id).PostAndReply(msg)
    let ( *->) (msg : 'Tin) (id : string)  : Async<'Tout> = Fredis.GetActor<'Tin, 'Tout>(id).PostAndReply(msg)

    let (->>-) (parent : string) (child : string)  = 
        Fredis.GetActor(parent).Link(Fredis.GetActor(child))

    let (-<<-) (child : string) (parent : string) = 
        Fredis.GetActor(parent).Link(Fredis.GetActor(child))

    let (->>=) (parent : string) (children : seq<string>)  = 
        let parent = Fredis.GetActor(parent)
        Seq.iter (fun child -> parent.Link(Fredis.GetActor(child)) |> ignore) children
        parent
        
