namespace Fredis

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading
open System.Diagnostics
open System.Threading
open System.Threading.Tasks

open BookSleeve



[<AutoOpenAttribute>]
module Utils =
    /// Option-coalescing operator
    let inline (??=) (opt:'a option) (fb:'a) = if opt.IsSome then opt.Value else fb


type Connection
    (host:string,?port:int,?ioTimeout:int,?password:string,?maxUnsent:int,?allowAdmin:bool,?syncTimeout:int,?maxPoolSize:int) as this =
    inherit RedisConnection(host,port??=6379,ioTimeout??=(-1),password??=null,maxUnsent??=Int32.MaxValue,allowAdmin??=false)
    
    let pool : ConnectionPool ref = ref (new ConnectionPool(host,port??=6379,ioTimeout??=(-1),password??=null,maxUnsent??=Int32.MaxValue,allowAdmin??=false,maxPoolSize??=2))
    
    do 
        base.Open().Wait() // |> ignore
        (!pool).TryReturn(this) |> ignore

    // BookSleeve recommends using the same connection for simple operations since they are thread safe
    // However some connections could be blocking (e.g. using blocking locks) or slow (e.g. complex lua scripts)
    // TODO Looks like the only resource BS will use during blocking ops is a socket

    /// Get an existing connection from a pool or a new connection to the same server with same parameters
    /// Use this method when a call to Redis could be blocking, e.g. when using distributed locks
    member this.Use with get() = lock pool (fun _ -> (!pool).Get() )

    /// Shortcut for Use() method
    static member (~+) (connection:Connection) = connection.Use

    interface IDisposable with
        member this.Dispose() =
            lock pool (
                fun _ -> if not ((!pool).TryReturn(this)) then this.Dispose()
            )


and [<Sealed;AllowNullLiteralAttribute>] internal ConnectionPool
    (host:string,?port:int,?ioTimeout:int,?password:string,?maxUnsent:int,?allowAdmin:bool,?syncTimeout:int,?maxPoolSize:int) =
    
    let port = defaultArg port 6379
    let ioTimeout = defaultArg ioTimeout -1
    let password = defaultArg password null
    let maxUnsent = defaultArg maxUnsent Int32.MaxValue
    let allowAdmin = defaultArg allowAdmin false
    let syncTimeout = defaultArg syncTimeout 10000
    let maxPoolSize = defaultArg maxPoolSize 1
    let pool = new BlockingCollection<Connection>(maxPoolSize)
    
    let tryAddNew () = 
        let conn = new Connection(host,port,ioTimeout,password,maxUnsent,allowAdmin,syncTimeout)
        if pool.TryAdd(conn) then 
            conn.Open().Wait() // |> ignore
            true
        else false

    member internal this.TryReturn(conn:Connection) = pool.TryAdd(conn) 
    member internal this.Get() : Connection = 
        let rec get() = 
            let succ, conn = pool.TryTake()
            if succ then 
                conn
            else
                tryAddNew() |> ignore
                get()
        get()

[<AutoOpenAttribute>]
module ConnectionModule =
    /// Get an existing connection from a pool or a new connection to the same server with same parameters
    /// Use this method when a call to Redis could be blocking, e.g. when using distributed locks
    let (~+) (conn:Connection) = conn.Use
    /// GetOpenSubscriberChannel on connection
    let (~%) (conn:Connection) = conn.GetOpenSubscriberChannel()
    /// Async await plain Task and return Async<unit>, to be used with do! inside Async
    let (!~)  (t: IAsyncResult) = t |> (Async.AwaitIAsyncResult >> Async.Ignore)
    /// Async await typed Task<'T> and return Async<'T>, to be used with let! inside Async
    let inline (!!)  (t: Task<'T>) = t |> Async.AwaitTask
    
    /// Run plain Task/IAsyncResult on current thread
    let (!~!)  (t: IAsyncResult) = t |> (Async.AwaitIAsyncResult >> Async.Ignore >> Async.StartImmediate)

    /// Run task Task<'T> on current thread and return results
    let inline (!!!)  (t: Task<'T>) = t.Result // |> (Async.AwaitTask >> Async.RunSynchronously)
    