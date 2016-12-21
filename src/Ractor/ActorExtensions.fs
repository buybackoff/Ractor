namespace Ractor

open System
open System.Linq
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Runtime.Caching
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open Ractor

// TODO copy docs comments from Actors
[<Extension>]
type Actors() =
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
                let dataConnection = this.RedisDataConnectionString
                let dataNameSapce = this.RedisDataNamespace
                let key = "(" + this.GetKey() + "->-" + continuation.GetKey() + ")"
                let computation : Message<'Task> * string -> Async<Message<'TResult2>> = 
                    fun (inMessage, resultId) -> 
                        async {
                            let rId1 = resultId + "-"
                            let rId2 = "-" + resultId
                            // 1. check if we have the final result. that could *very rarely* happen if 
                            // we resume work from dead continuator while actor1 and actor2 are alive
                            // (it takes a local cache lookup - very cheap)
                            let secondIsDone, r2 = actor2.TryGetResultIfItDefinitelyExists(rId2) // resultId
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
                                let firstIsDone, _ = actor1.TryGetResultIfItDefinitelyExists(rId1) //key + "|" + resultId
                                if firstIsDone then // extremely unusual
                                    // The first result must be already in the second inbox/pipeline
                                    // Do not need to get the intermediate result here, do nothing
                                    ()
                                else // normal case
                                    // here the task message is sent to the first actor and 
                                    // it will arrive to the results list and stay there
                                    // before the results timeout

                                    // first actor has a prefixed resultId
                                    let envelope1 : Envelope<'Task> = Envelope(inMessage, rId1, [| actor2.Id |]) //key + "|" + resultId
                                    do! actor1.Post(envelope1) |> Async.Ignore
                                    // do not wait for intermediate result
                                    ()
                                // wait for second actor
                                let! r2 = actor2.GetResultAsync(rId2) //resultId
                                return r2
                        }
            
                let result = 
                    { new Actor<'Task, 'TResult2>() with
                           override __.RedisDataConnectionString with get() = dataConnection
                           override __.RedisDataNamespace with get() = dataNameSapce
                           override __.RedisConnectionString with get() = actor1.RedisConnectionString
                           override __.GetKey() = key
                           override __.ResultTimeout with get() = this.ResultTimeout + continuation.ResultTimeout
                           //instead of [override __.Computation(input) = ...] assign internal computation directly
                    }
                // overwrite the default computation, which ignores resultId, with the proper one
                result.ExtendedComputation <- computation
                result

    [<Extension>]
    static member ParallelWith(this : Actor<'Task, 'TResult>, second : Actor<'Task2, 'TResult2>) 
            : Actor<'Task * 'Task2, 'TResult * 'TResult2> =
                let actor1 = ActorImpl<'Task, 'TResult>.Instance(this)
                let actor2 = ActorImpl<'Task2, 'TResult2>.Instance(second)
                let dataConnection = this.RedisDataConnectionString
                let dataNameSapce = this.RedisDataNamespace
                let timeout = Math.Max(this.ResultTimeout, second.ResultTimeout)
                let key = "(" + this.GetKey() + "|>|" + second.GetKey() + ")"
                let computation : Message<'Task * 'Task2> * string -> Async<Message<'TResult * 'TResult2>> = 
                    fun (inMessage, resultId) ->
                        async {
                            let (t1 : 'Task), (t2:'Task2) = inMessage.Value
                            let rId1 = resultId + "|"
                            let rId2 = "|" + resultId
                            let envelope1 : Envelope<'Task> = Envelope(Message(t1, false, null), rId1, [||])
                            let envelope2 : Envelope<'Task2> = Envelope(Message(t2, false, null), rId2, [||])
                            let! child1 = Async.StartChild( actor1.PostAndGetResult(envelope1), timeout)
                            let! child2 = Async.StartChild( actor2.PostAndGetResult(envelope2), timeout)
                            let! r1 = child1
                            let! r2 = child2
                            let result = r1.Value, r2.Value
                            let hasError = r1.HasError || r2.HasError
                            let ex : Exception = 
                                match r1.Error, r2.Error with
                                | null, null -> null
                                | null, e2 -> e2
                                | e1, null -> e1
                                | e1, e2 -> System.AggregateException(e1, e2) :> Exception
                            let outMessage = Message(result, hasError, ex)
                            return outMessage
                        }
            
                let result = 
                    { new Actor<'Task * 'Task2, 'TResult * 'TResult2>() with
                           override __.RedisDataConnectionString with get() = dataConnection
                           override __.RedisDataNamespace with get() = dataNameSapce
                           override __.RedisConnectionString with get() = actor1.RedisConnectionString
                           override __.GetKey() = key
                           override __.ResultTimeout with get() = timeout
                           //instead of [override __.Computation(input) = ...] assign internal computation directly
                    }
                // overwrite the default computation, which ignores resultId, with the proper one
                result.ExtendedComputation <- computation
                result

namespace Ractor.FSharp
[<AutoOpenAttribute>]
module FSharpActorExtensions =
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
                let dataConnection = this.RedisDataConnectionString
                let dataNameSapce = this.RedisDataNamespace
                let key = "(" + this.GetKey() + "->-" + continuation.GetKey() + ")"
                let computation : Message<'Task> * string -> Async<Message<'TResult2>> = 
                    // this resultId is passed from continuator call and it is in ShortGuid format
                    fun (inMessage, resultId) -> 
                        async {
                            let rId1 = resultId + "-"
                            let rId2 = "-" + resultId
                            // 1. check if we have the final result. that could *very rarely* happen if 
                            // we resume work from dead continuator while actor1 and actor2 are alive
                            // (it takes a local cache lookup - very cheap)
                            let secondIsDone, r2 = actor2.TryGetResultIfItDefinitelyExists(rId2) //resultId
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
                                let firstIsDone, _ = actor1.TryGetResultIfItDefinitelyExists(rId1) // key + "|" + resultId
                                if firstIsDone then // extremely unusual
                                    // The first result must be already in the second inbox/pipeline
                                    // Do not need to get the intermediate result here, do nothing
                                    ()
                                else // normal case
                                    // here the task message is sent to the first actor and 
                                    // it will arrive to the results list and stay there
                                    // before the results timeout

                                    // first actor has a prefixed resultId
                                    let envelope1 : Envelope<'Task> = Envelope(inMessage, rId1, [| actor2.Id |]) // key + "|" + resultId
                                    do! actor1.Post(envelope1) |> Async.Ignore
                                    // do not wait for intermediate result
                                    ()
                                // wait for second actor
                                let! r2 = actor2.GetResultAsync(rId2) // resultId
                                return r2
                        }
            
                let result = 
                    { new Actor<'Task, 'TResult2>() with
                           override __.RedisDataConnectionString with get() = dataConnection
                           override __.RedisDataNamespace with get() = dataNameSapce
                           override __.RedisConnectionString with get() = actor1.RedisConnectionString
                           override __.GetKey() = key
                           override __.ResultTimeout with get() = this.ResultTimeout + continuation.ResultTimeout
                           //instead of [override __.Computation(input) = ...] assign internal computation directly
                    }
                // overwrite the default computation, which ignores resultId, with the proper one
                result.ExtendedComputation <- computation
                result
        //#endregion

        member this.ParallelWith<'Task,'TResult,'Task2,'TResult2>
            (second : Actor<'Task2, 'TResult2>) 
            : Actor<'Task * 'Task2, 'TResult * 'TResult2> =
                let actor1 = ActorImpl<'Task, 'TResult>.Instance(this)
                let actor2 = ActorImpl<'Task2, 'TResult2>.Instance(second)
                let dataConnection = this.RedisDataConnectionString
                let dataNameSapce = this.RedisDataNamespace
                let timeout = Math.Max(this.ResultTimeout, second.ResultTimeout)
                let key = "(" + this.GetKey() + "|>|" + second.GetKey() + ")"
                let computation : Message<'Task * 'Task2> * string -> Async<Message<'TResult * 'TResult2>> = 
                    fun (inMessage, resultId) -> 
                        async {
                            let (t1 : 'Task), (t2:'Task2) = inMessage.Value
                            let rId1 = resultId + "|"
                            let rId2 = "|" + resultId
                            let envelope1 : Envelope<'Task> = Envelope(Message(t1, false, null), rId1, [||])
                            let envelope2 : Envelope<'Task2> = Envelope(Message(t2, false, null), rId2, [||])
                            let! child1 = Async.StartChild( actor1.PostAndGetResult(envelope1), timeout)
                            let! child2 = Async.StartChild( actor2.PostAndGetResult(envelope2), timeout)
                            let! r1 = child1
                            let! r2 = child2
                            let result = r1.Value, r2.Value
                            let hasError = r1.HasError || r2.HasError
                            let ex : Exception = 
                                match r1.Error, r2.Error with
                                | null, null -> null
                                | null, e2 -> e2
                                | e1, null -> e1
                                | e1, e2 -> System.AggregateException(e1, e2) :> Exception
                            let outMessage = Message(result, hasError, ex)
                            return outMessage
                        }
            
                let result = 
                    { new Actor<'Task * 'Task2, 'TResult * 'TResult2>() with
                           override __.RedisDataConnectionString with get() = dataConnection
                           override __.RedisDataNamespace with get() = dataNameSapce
                           override __.RedisConnectionString with get() = actor1.RedisConnectionString
                           override __.GetKey() = key
                           override __.ResultTimeout with get() = timeout
                           //instead of [override __.Computation(input) = ...] assign internal computation directly
                    }
                // overwrite the default computation, which ignores resultId, with the proper one
                result.ExtendedComputation <- computation
                result

[<AutoOpenAttribute>]
module Operators =
    /// <summary>
    /// Continue with
    /// </summary>
    let inline (->-) (first: Actor<'Task, 'TResult1>) (continuation : Actor<'TResult1, 'TResult2>) =
           first.ContinueWith(continuation)

    /// <summary>
    /// Parallel with
    /// </summary>
    let inline (|^|) (first: Actor<'Task, 'TResult>) (second : Actor<'Task2, 'TResult2>) =
           first.ParallelWith(second)
            