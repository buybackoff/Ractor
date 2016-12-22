namespace Ractor

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System
open System.Runtime.InteropServices
open System.Runtime.CompilerServices


type Message<'T>(value:'T, hasError:bool, error:Exception) = 
    member val Value = value with get, set
    member val HasError = hasError with get, set
    member val Error = error with get, set
    new() = Message<'T>(Unchecked.defaultof<'T>, false, null)

type Envelope<'Task>(message:Message<'Task>, resultId:string, callerIds : String[]) = 
    member val Message = message with get, set
    member val ResultId = resultId  with get, set
    member val CallerIds : String[] = callerIds with get, set
    new() = Envelope<'Task>(Unchecked.defaultof<Message<'Task>>, "", [||])


type IRactorPerformanceMonitor =
    abstract AllowHighPriorityActors : unit -> bool
    abstract AllowLowPriorityActors : unit -> bool
    abstract PeriodMilliseconds : int with get



//type AsyncAutoResetEvent2 (init:bool) =
//    let sem = new SemaphoreSlim(1,1)
//    do
//      if not init then if not <| sem.Wait(0) then failwith "Could not set semaphore to zero"
//    
//    member this.WaitAsync() = sem.WaitAsync()
//    member this.WaitAsync(timeout:int) = sem.WaitAsync(timeout)
//
//    member this.Set() = sem.Release()
//    member this.Reset() = if not <| sem.Wait(0) then failwith "Could not set semaphore to zero"
//
//    interface IDisposable with
//      member __.Dispose() = sem.Dispose()
//
//    new() = new AsyncAutoResetEvent2(true)




type AsyncManualResetEvent internal (state:bool option) =
    //http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266920.aspx
    [<VolatileFieldAttribute>]
    let mutable m_tcs = TaskCompletionSource<bool>()
    do 
      if state.IsSome then m_tcs.SetResult(state.Value)

    member this.WaitAsync() : Task = m_tcs.Task :> Task
    
    //member this.WaitAsync(timeout:int) = m_tcs.Task.WithTimeout(timeout)

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
    new() = AsyncManualResetEvent(None)
    new(state:bool) = AsyncManualResetEvent(Some(state))

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
                let mutable registration = Unchecked.defaultof<_>
                registration <- ct.Token.Register(Action(fun _ -> tcs.TrySetResult(false) |> ignore; registration.Dispose();ct.Dispose()))
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


[<AutoOpenAttribute>]
module unnestModule = 
    let unnest3 (tuple : (('T1 * 'T2) * 'T3)  ) =
        let ((v1, v2), v3) = tuple
        v1, v2, v3

    let unnest4 (tuple : ((('T1 * 'T2) * 'T3) * 'T4)  ) =
        let (((v1, v2), v3), v4) = tuple
        v1, v2, v3, v4

    let unnest5 (tuple : (((('T1 * 'T2) * 'T3) * 'T4) * 'T5)  ) =
        let ((((v1, v2), v3), v4), v5) = tuple
        v1, v2, v3, v4, v5

    let unnest6 (tuple : ((((('T1 * 'T2) * 'T3) * 'T4) * 'T5) * 'T6)  ) =
        let (((((v1, v2), v3), v4), v5), v6) = tuple
        v1, v2, v3, v4, v5, v6

    let unnest7 (tuple : (((((('T1 * 'T2) * 'T3) * 'T4) * 'T5) * 'T6) * 'T7)  ) =
        let ((((((v1, v2), v3), v4), v5), v6), v7) = tuple
        v1, v2, v3, v4, v5, v6, v7



[<Extension>]
type TuplesExtension() =
    [<Extension>]
    static member Unnest(this : (('T1 * 'T2) * 'T3)) = 
        unnest3 this

    [<Extension>]
    static member Unnest(this : (('T1 * 'T2) * 'T3) * 'T4 ) = 
        unnest4 this

    [<Extension>]
    static member Unnest(this : (((('T1 * 'T2) * 'T3) * 'T4) * 'T5) ) = 
        unnest5 this

    [<Extension>]
    static member Unnest(this : ((((('T1 * 'T2) * 'T3) * 'T4) * 'T5) * 'T6) ) = 
        unnest6 this

    [<Extension>]
    static member Unnest(this : (((((('T1 * 'T2) * 'T3) * 'T4) * 'T5) * 'T6) * 'T7) ) = 
        unnest7 this



[<AutoOpenAttribute>]
module TaskModule =
  // Reduced (compared to fsharpx) implementation from Spreads
  let trueTask = Task.FromResult(true)
  let falseTask = Task.FromResult(false)
  let cancelledBoolTask = 
    let tcs = new TaskCompletionSource<bool>()
    tcs.SetCanceled()
    tcs.Task

  let inline bind  (f: 'T -> Task<'U>) (m: Task<'T>) =
      if m.IsCompleted then f m.Result
      else
        let tcs = (Runtime.CompilerServices.AsyncTaskMethodBuilder<_>.Create()) // new TaskCompletionSource<_>() // NB do not allocate objects
        let t = tcs.Task
        let awaiter = m.GetAwaiter() // NB this is faster than ContinueWith
        awaiter.OnCompleted(fun _ -> tcs.SetResult(f m.Result))
        t.Unwrap()

  let inline bindTimeout  (f: 'T -> Task<'U>) (m: Task<'T>) (timeout:int) =
      if m.IsCompleted then f m.Result
      else
        let tcs = new TaskCompletionSource<Task<'U>>()
        let t = tcs.Task
        let ct = new CancellationTokenSource(timeout)
        let mutable registration = Unchecked.defaultof<_>
        registration <- ct.Token.Register(Action(fun _ -> tcs.TrySetException(TimeoutException()) |> ignore; registration.Dispose();ct.Dispose()))
        let awaiter = m.GetAwaiter()
        let u = f m.Result
        awaiter.OnCompleted(fun _ -> tcs.TrySetResult(f m.Result) |> ignore)
        t.Unwrap()

  let inline doBind  (f: unit -> Task<'U>) (m: Task) =
      if m.IsCompleted then f()
      else
        let tcs = (Runtime.CompilerServices.AsyncTaskMethodBuilder<_>.Create())
        let t = tcs.Task
        let awaiter = m.GetAwaiter()
        awaiter.OnCompleted(fun _ -> tcs.SetResult(f()))
        t.Unwrap()

  let inline returnM a = Task.FromResult(a)
  let inline returnMUnit () = trueTask :> Task

  let inline bindBool  (f: bool -> Task<bool>) (m: Task<bool>) =
      if m.IsCompleted then f m.Result
      else
        let tcs = (Runtime.CompilerServices.AsyncTaskMethodBuilder<_>.Create()) // new TaskCompletionSource<_>() // NB do not allocate objects
        let t = tcs.Task
        let awaiter = m.GetAwaiter() // NB this is faster than ContinueWith
        awaiter.OnCompleted(fun _ -> tcs.SetResult(f m.Result))
        t.Unwrap()

  let inline returnMBool (a:bool) = if a then trueTask else falseTask
    
  type TaskBuilder(?continuationOptions, ?scheduler, ?cancellationToken) =
      let contOptions = defaultArg continuationOptions TaskContinuationOptions.None
      let scheduler = defaultArg scheduler TaskScheduler.Default
      let cancellationToken = defaultArg cancellationToken CancellationToken.None

      member this.Return x = returnM x
      //member this.Return () = returnMUnit ()

      member this.Zero() = returnM()

      member this.ReturnFrom (a: Task<'T>) = a
      //member this.ReturnFrom (a: Task) = a

      member this.Bind(m:Task<'m>, f:'m->Task<'n>) = bind f m // bindWithOptions cancellationToken contOptions scheduler f m
           
      member this.Bind(m:Task, f:unit-> Task<'j>) = doBind f m

      member this.Combine(comp1:Task<'m>, comp2:'m->Task<'n>) =
          this.Bind(comp1, comp2)

      member this.While(guard, m) =
          let rec whileRec(guard, m) = 
            if not(guard()) then this.Zero() else
                this.Bind(m(), fun () -> whileRec(guard, m))
          whileRec(guard, m)

      member this.While(guardTask:unit->Task<bool>, body) =
        let m = guardTask()
        let onCompleted() =
          this.Bind(body(), fun () -> this.While(guardTask, body))
        if m.Status = TaskStatus.RanToCompletion then 
          onCompleted()
        else
          let tcs =  new TaskCompletionSource<_>() // (Runtime.CompilerServices.AsyncTaskMethodBuilder<_>.Create())
          let t = tcs.Task
          let awaiter = m.GetAwaiter()
          awaiter.OnCompleted(fun _ -> 
            if m.IsFaulted then
              tcs.SetException(m.Exception)
            elif m.IsCanceled then
              tcs.SetCanceled()
            else
              tcs.SetResult(onCompleted())
            )
          t.Unwrap()

      member this.TryWith(body:unit -> Task<_>, catchFn:exn -> Task<_>) =  
        try
            body()
              .ContinueWith(fun (t:Task<_>) ->
                  match t.IsFaulted with
                  | false -> returnM(t.Result)
                  | true  -> catchFn(t.Exception.GetBaseException()))
              .Unwrap()
        with e -> catchFn(e)

      member this.TryFinally(m, compensation) =
          try this.ReturnFrom m
          finally compensation()

      member this.TryFinally(f, compensation) =
          try this.ReturnFrom (f())
          finally compensation()

//      member this.TryFinally(m:Task, compensation) =
//          try this.ReturnFrom m
//          finally compensation()

      member this.Using(res: #IDisposable, body: #IDisposable -> Task<_>) =
          this.TryFinally(body res, fun () -> match res with null -> () | disp -> disp.Dispose())

      member this.For(sequence: seq<_>, body) =
          this.Using(sequence.GetEnumerator(),
                                fun enum -> this.While(enum.MoveNext, fun () -> body enum.Current))

      member this.Delay (f: unit -> Task<'T>) = f

      member this.Run (f: unit -> Task<'T>) = f()

  let task = TaskBuilder(scheduler = TaskScheduler.Current)



  [<Extension>]
  type TaskExtensions() =
      [<Extension>]
      static member WithTimeout(this : Task<'T>, timeout:int) : Task<'T> = 
        if this.IsCompleted then this
        else
          task {
            let delay = Task.Delay(timeout)
            let! child = Task.WhenAny(this, delay)
            if child <> delay then return this.Result
            else return raise (TimeoutException()) 
          }

      [<Extension>]
      static member WithTimeout(this : Task, timeout:int) : Task<bool> = 
        if this.IsCompleted then trueTask
        else
          task {
            let delay = Task.Delay(timeout)
            let! child = Task.WhenAny(this, delay)
            if child <> delay then return! trueTask
            else return! falseTask
          }
