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

[<Extension>]
type Actors() =
    [<Extension>]
    static member Start(this : Actor<'TInput, 'TResult>) : unit = 
        let actor = ActorImpl<'TInput, 'TResult>.Instance(this)
        actor.Start()
    [<Extension>]
    static member Stop(this : Actor<'TInput, 'TResult>) : unit = 
        let actor = ActorImpl<'TInput, 'TResult>.Instance(this)
        actor.Stop()


    [<Extension>]
    static member PostAsync<'TInput, 'TResult  when 'TResult : not struct>
      (this : Actor<'TInput, 'TResult>, message : 'TInput) : Task<string> =
      let actor = ActorImpl<'TInput, 'TResult>.Instance(this)
      let message = new Message<_>(message, false, null)
      //let envelope = new Envelope
      actor.PostAsync(message) |> Async.StartAsTask

    [<Extension>]
    static member GetResultAsync(this : Actor<'TInput, 'TResult>, resultId : string) : Task<'TResult> = 
      let actor = ActorImpl<'TInput, 'TResult>.Instance(this)
      let t = 
        async {
          let! result = actor.GetResultAsync(resultId)
          return result.AsTask()
        } |> Async.StartAsTask
      t.Unwrap()

    [<Extension>]
    static member PostAndGetResultAsync<'TInput, 'TResult when 'TResult : not struct>
      (this : Actor<'TInput, 'TResult>, message : 'TInput) : Task<'TResult> =
      let actor = ActorImpl<'TInput, 'TResult>.Instance(this)
      let t = 
        async {
          let message = new Message<_>(message, false, null)
          let! (result : Message<'TResult>) = actor.PostAndGetResultAsync(message)
          return result.Value
        } |> Async.StartAsTask
      t
