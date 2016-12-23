#nowarn "760" // new for IDisposable

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

// TODO add cancellation token to computation
// TODO Error handling

[<AbstractClassAttribute>]
type Actor<'TInput, 'TResult when 'TResult : not struct>()  as this = 
    inherit ActorBase()

    abstract RedisConnectionString : string with get
    override this.RedisConnectionString with get() =  "localhost"
    
    /// <summary>
    /// Actor unique identificator (name)
    /// </summary>
    abstract Id : string with get
    override val Id = this.GetType().Name with get

    /// <summary>
    /// Logical group of an Actor. Used for sharding when Redis cluster is used
    /// </summary>
    abstract Group : string with get
    override val Group = "" with get

    abstract Computation : 'TInput -> Task<'TResult>
    override this.Computation(input) = 
        let tcs = TaskCompletionSource()
        tcs.SetResult(Unchecked.defaultof<'TResult>)
        tcs.Task
    
    /// <summary>
    /// Time in milliseconds to wait for computation to finish and to wait before discarding unclaimed results.
    /// </summary>
    abstract ResultTimeout : int with get
    override this.ResultTimeout with get() =  60000
    
    /// <summary>
    /// Priority, from 0 - highest to int.Max - lowest.
    /// </summary>
    abstract Priority : int with get
    override this.Priority with get() =  0
    
    /// <summary>
    /// Maximum number of Actor executing concurrently per CPU.
    /// For CPU-intensive tasks this should be 1, for pure-IO tasks
    /// it could be as high as the maximum 255.
    /// </summary>
    abstract MaxConcurrencyPerCpu : uint8 with get
    override this.MaxConcurrencyPerCpu with get() = 1uy

    abstract AutoStart : bool with get
    override this.AutoStart with get() = false


type internal ActorImpl<'TInput, 'TResult> when 'TResult : not struct
    internal (definition:Actor<'TInput, 'TResult>) as this =

    let conn = 
      if String.IsNullOrWhiteSpace(definition.RedisConnectionString) then
          if String.IsNullOrWhiteSpace(ActorImpl<_,_>.DefaultRedisConnectionString) then
              raise (new ArgumentException("Redis connection string is not set"))
          else
              ActorImpl<_,_>.DefaultRedisConnectionString
      else definition.RedisConnectionString

    let redis = Connections.GetOrCreateRedis(conn, "R")

    let maximumConcurrency = Environment.ProcessorCount * int definition.MaxConcurrencyPerCpu
    let semaphore = new SemaphoreSlim(maximumConcurrency, maximumConcurrency)

    let mutable started = false
    let mutable cts = new CancellationTokenSource()
    
    let queue = new RedisQueue<Message<'TInput>>(redis, definition.Id, definition.ResultTimeout, definition.Group)
    let results = new RedisAsyncDictionary<Message<'TResult>>(redis, definition.Id, definition.ResultTimeout, definition.Group)

    //let errorsKey = prefix + ":errors"

   
    static let actors = Dictionary<string, obj>()

    do
      redis.Serializer <- JsonSerializer()
      if definition.AutoStart then this.Loop()

    static member val DefaultRedisConnectionString = "" with get, set
    static member ActorsRepo with get () = actors
    static member Instance<'TInput, 'TResult>(definition:Actor<'TInput, 'TResult>) : ActorImpl<'TInput, 'TResult> = 
      let actor =
        if ActorImpl<_,_>.ActorsRepo.ContainsKey(definition.Id) then
          Debug.WriteLine("Took existing actor: " + definition.Id)
          ActorImpl<_,_>.ActorsRepo.[definition.Id] :?> ActorImpl<'TInput, 'TResult>
        else
          let a' = ActorImpl(definition)
          ActorImpl<_,_>.ActorsRepo.[definition.Id] <- a'
          a'
      actor
    
    member internal this.Id = definition.Id
    member internal this.RedisConnectionString = definition.RedisConnectionString
    member internal this.Computation = definition.Computation
    member internal this.ResultTimeout = definition.ResultTimeout
   
    member this.Start() : unit = if not started then this.Loop()
    
    member this.Stop() = 
      if started then 
        started <- false
        cts.Cancel |> ignore
    
    member private this.Loop() =
      task {
        // while not cancelled
        while not cts.IsCancellationRequested do
          // try enter into semaphore until concurrency limits allow
          do! semaphore.WaitAsync(cts.Token)
          // wait for a message on the default scheduler 
          // the semaphore controls concurrency here and receiving is cheap
          // our goal is to avoid consuming too much tasks without ability to process them
          // while other workers are waiting
          let! (result: QueueReceiveResult<Message<'TInput>>) = queue.TryReceiveMessage()
          if result.Ok then
            // start computation on a special priority task scheduler
            let message = result.Value.Value
            // TODO Custom priority factory here
            Task.Factory.StartNew(fun _ -> definition.Computation(message)).Unwrap()
            // continue with posting result -> deleting handle -> releasing semaphore
              .ContinueWith(fun (t:Task<'TResult>) ->
                try
                  let resMessage = Message<'TResult>.FromTask(t)
                  results.TryFill(result.Id, resMessage).Wait()
                  queue.TryDeleteMessage(result.DeleteHandle).Wait()
                  ()
                finally
                  semaphore.Release() |> ignore
              ) |> ignore
            ()
          else ()
      } |> ignore

    // the three methods below is all it gets to post a message and get results back


    /// <summary>
    /// Post an envelope to the queue
    /// </summary>
    member internal this.PostAsync<'TInput>(message : Message<'TInput>) : Async<string> =
      let sendResult = 
        task {
          let! (result : QueueSendResult) = queue.TrySendMessage(message)
          return result.Id
        } |> Async.AwaitTask
      sendResult
        
    /// <summary>
    /// Wait for a result and return it.
    /// </summary>
    /// <param name="resultId"></param>
    member internal this.GetResultAsync(resultId : string) : Async<Message<'TResult>> =
      let sendResult = 
        task {
          let! result = results.TryTake(resultId)
          return result
        } |> Async.AwaitTask
      sendResult

    member internal this.PostAndGetResultAsync(envelope : Message<'TInput>) : Async<Message<'TResult>> = 
      let standardCall = async {
          let! resultId = this.PostAsync(envelope)
          return! this.GetResultAsync(resultId)
        }
      standardCall


    // interfaces 

    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            cts.Dispose()
            queue.Dispose()
            results.Dispose()



