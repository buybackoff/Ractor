namespace Fredis

open System
open System.Threading.Tasks

// DO NOT! Include interfaces of IRedis, IPocoPersistor, IBlobPersistor etc in constructor
// these tool are convenient to work with POCOs, persistence, cache - keep then decoupled
// Instead provide Redis connection string to Fredis object and use "fredis" namespace
// also use FsPickler for binary serialization of messages

// add methods GetRedis("name" = null, default = true), RedisterRedis("name" = null) - with additional Redises for 
// working set
// same for GetDBPersistor, GetBlob()

// from within Fredis Actor system use internal one, but in Actors behaviours use Get...("") methods
// for manipulating working set data
// Fredis's redis could deal with references to working set without copying all data

// limit on workers per application - 64 is in PLINQ but if they all are async and not CPU bound, 
// should set empirically
// or could use CPU utilization - do not consume task from Redis if CPU > 90%
// will have to monitor # of concurrent workers anyway... http://stackoverflow.com/a/2608758/801189


// actors are named


// App servers vs fronends
// Post schedules work somewhere, it could be done on app servers
// PostGetReply could be also done "somewhere" but it is better to have reply ASAP
// Receive - every worker does it
// PostGetReply is automatically high priority
// need to check if capacity is exhausted and prioritize high priority jobs
// job without reply could also be high priority, but the same prioritization logics will work on app servers
// "more of the same things" Pinterest philosofy: app servers are the same as front end
// but app servers do not get requests, they never do any root post, they only receive from Fredis
// 

type Fredis(connectionString:string)=
    let redis = Redis(connectionString, "Fredis")
    do
        redis.Serializer <- Serialisers.Pickler

    member __.StartActor<'Tin,'Tout>(id:string, computation:'Tin -> Async<'Tout>) = ()
    member __.StartActor<'Tin,'Tout>(id:string, computation:Func<'Tin,Task<'Tout>>) = ()

    member __.GetActor(id:string) = ()

    member __.Post(id:string, message:'Tin) : unit = ()
    member __.PostAndReply(id:string, message:'Tin) : 'Tout = ()