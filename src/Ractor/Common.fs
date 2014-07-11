namespace Ractor

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks


type internal Message<'T>=
| Value of 'T
| Error of Exception

type internal Envelope<'Task> = Message<'Task> * string * string [] // * DateTime monitor time from post to get and time for pure computation



type IRactorPerformanceMonitor =
    abstract AllowHighPriorityActors : unit -> bool
    abstract AllowLowPriorityActors : unit -> bool
    abstract PeriodMilliseconds : int with get



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
                ct.Token.Register(Action(fun _ -> tcs.TrySetResult(false) |> ignore)) |> ignore
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
            if toRelease <> null then toRelease.TrySetResult(true) |> ignore
        finally
            Monitor.Exit(m_waits)



[<AutoOpenAttribute>]
module Helpers =
    
    let isSubclassOfRawGeneric(generic: Type, toCheck: Type) : bool =
        let mutable toCheck = toCheck
        let mutable res = false
        while (toCheck <> null && toCheck <> typedefof<obj>) do
            let cur = if toCheck.IsGenericType then toCheck.GetGenericTypeDefinition() else toCheck
            if generic = cur then
                res <- true
            toCheck <- toCheck.BaseType
        res

    [<ObsoleteAttribute>]
    let inline deleteRepeatingItemsInHSET (redis:Redis, hkey:string) =
        // self-containig script from params
        // set current items
        // if previous items exist, take intersect
        let lua = 
            @"  local previousKey = KEYS[1]..':previousKeys'
                local currentKey = KEYS[1]..':currentKeys'
                local currentItems = redis.call('HKEYS', KEYS[1])
                local res = 0
                redis.call('DEL', currentKey)
                if redis.call('HLEN', KEYS[1]) > 0 then
                   redis.call('SADD', currentKey, unpack(currentItems))
                   local intersect
                   if redis.call('SCARD', previousKey) > 0 then
                       intersect = redis.call('SINTER', previousKey, currentKey)
                       if #intersect > 0 then
                            redis.call('HDEL', KEYS[1], unpack(intersect))
                            res = #intersect
                       end
                   end
                end
                redis.call('DEL', previousKey)
                if #currentItems > 0 then
                    redis.call('SADD', previousKey, unpack(currentItems))
                end
                return res
            "
        Console.WriteLine("Before eval")
        redis.EvalAsync<int>(lua, [|hkey|]).Result
