namespace Fredis

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Diagnostics
open System.Threading
open System.Threading.Tasks


[<AutoOpenAttribute>]
module internal Utils =
    /// Option-coalescing operator
    let inline (??=) (opt:'a option) (fallback:'a) = if opt.IsSome then opt.Value else fallback

    /// Async await plain Task and return Async<unit>, to be used with do! inside Async
    [<ObsoleteAttribute>]
    let (!~)  (t: IAsyncResult) = t |> (Async.AwaitIAsyncResult >> Async.Ignore)
    /// Async await typed Task<'T> and return Async<'T>, to be used with let! inside Async
    [<ObsoleteAttribute>]
    let inline (!!)  (t: Task<'T>) = t |> Async.AwaitTask
    /// Run plain Task/IAsyncResult on current thread
    [<ObsoleteAttribute>]
    let (!~!)  (t: IAsyncResult) = t |> (Async.AwaitIAsyncResult >> Async.Ignore >> Async.RunSynchronously)
    /// Run task Task<'T> on current thread and return results
    [<ObsoleteAttribute>]
    let inline (!!!)  (t: Task<'T>) = t.Result // |> (Async.AwaitTask >> Async.RunSynchronously)
    

type IFredisPerformanceMonitor =
    abstract AllowLowPriorityActors : unit -> bool
    abstract FrequencySeconds : int with get

