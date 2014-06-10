namespace Fredis

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type IFredisPerformanceMonitor =
    abstract AllowHighPriorityActors : unit -> bool
    abstract AllowLowPriorityActors : unit -> bool
    abstract PeriodMilliseconds : int with get


type internal ExceptionInfo<'T> = 
    | ExceptionInfo of string * 'T * Exception


type AsyncManualResetEvent () =
    
    //http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266920.aspx

    [<VolatileFieldAttribute>]
    let mutable m_tcs = TaskCompletionSource<bool>()

    member this.WaitAsync() = m_tcs.Task
    member this.Set() = m_tcs.TrySetResult(true)
    member this.Reset() =
            let rec loop () =
                let tcs = m_tcs
                if not tcs.Task.IsCompleted || 
                    Interlocked.CompareExchange(&m_tcs, new TaskCompletionSource<bool>(), tcs) = tcs then
                    ()
                else
                    loop()
            loop ()

type AsyncAutoResetEvent () =
    //http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266923.aspx
    static let mutable s_completed = Task.FromResult(true)
    let m_waits = new Queue<TaskCompletionSource<bool>>()
    let mutable m_signaled = false

    member this.WaitAsync(timeout:int) = 
        Monitor.Enter(m_waits)
        try
            if m_signaled then
                m_signaled <- false
                s_completed
            else
                let ct = new CancellationTokenSource(timeout)
                let tcs = new TaskCompletionSource<bool>()
                ct.Token.Register(Action(fun _ -> tcs.SetResult(false))) |> ignore
                m_waits.Enqueue(tcs)
                tcs.Task
        finally
            Monitor.Exit(m_waits)

    member this.Set() = 
        let mutable toRelease = Unchecked.defaultof<TaskCompletionSource<bool>>
        Monitor.Enter(m_waits)
        try
            if m_waits.Count > 0 then
                toRelease <- m_waits.Dequeue() 
            else 
                if not m_signaled then m_signaled <- true
            if toRelease <> null then toRelease.SetResult(true)
        finally
            Monitor.Exit(m_waits)



[<Obsolete>] // this is not for auto, but for manual when return result only once + reset
//TODO manual
// here in Set should use queue because multiple sets will overwrite result
type AsyncAutoResultWaiter<'TResult> () =
    
    //http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266923.aspx

    let defaultValue:'TResult = Unchecked.defaultof<'TResult>
    let m_waits = new Queue<TaskCompletionSource<'TResult>>()
    let mutable m_signaled = defaultValue

    member this.WaitAsync(timeout:int) = 
        Monitor.Enter(m_waits)
        try
            if not (Object.Equals(m_signaled, defaultValue)) then
                let result = m_signaled
                m_signaled <- defaultValue
                Task.FromResult(result)
            else
                let ct = new CancellationTokenSource(timeout)
                let tcs = new TaskCompletionSource<'TResult>()
                ct.Token.Register(Action(fun _ -> tcs.SetResult(defaultValue))) |> ignore
                m_waits.Enqueue(tcs)
                tcs.Task
        finally
            Monitor.Exit(m_waits)

    member this.Set(result:'TResult) = 
        let mutable toRelease = Unchecked.defaultof<TaskCompletionSource<'TResult>>
        Monitor.Enter(m_waits)
        try
            if m_waits.Count > 0 then
                toRelease <- m_waits.Dequeue() 
            else 
                if not (Object.Equals(m_signaled, defaultValue)) then m_signaled <- result
            if toRelease <> null then toRelease.SetResult(result)
        finally
            Monitor.Exit(m_waits)