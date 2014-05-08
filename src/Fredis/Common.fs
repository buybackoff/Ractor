namespace Fredis

open System
open System.Threading
open System.Threading.Tasks

type IFredisPerformanceMonitor =
    abstract AllowLowPriorityActors : unit -> bool
    abstract FrequencySeconds : int with get


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
