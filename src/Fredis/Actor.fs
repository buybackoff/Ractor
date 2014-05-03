namespace Fredis

open System
open System.Collections.Generic
open System.Threading
open Fredis

// TODO local queue

type Actor<'Tin, 'Tout> private (redis : Redis, id : string, ?computation : 'Tin -> Async<'Tout>) = 
    let children = Dictionary<string, Actor<'Tout, _>>()
    let mutable started = false
    let mutable cts = Unchecked.defaultof<CancellationTokenSource>
    let awaitMessageHandle = new AutoResetEvent(false)
    let awaitResultHandle = new AutoResetEvent(false)
    let prefix = id + ":Mailbox:"
    let inboxKey = prefix + ":inbox"
    let pipelineKey = prefix + ":pipeline"
    let resultsKey = prefix + ":results"
    let channelKey = prefix + ":channel"
    let errorsKey = prefix + ":errors"
    let mutable errorHandler = Unchecked.defaultof<Actor<ExceptionInfo<'Tin>, _>>
    
    // global limit for number of tasks
    static let semaphor = new SemaphoreSlim(256)

    do 
        redis.Subscribe(channelKey, 
                        Action<string, string>(fun channel message -> 
                            match message with
                            | "" -> awaitMessageHandle.Set() |> ignore
                            | x -> awaitResultHandle.Set() |> ignore))
    
    let rec await timeout = 
        async { 
            //let! message = !!redis.RPopAsync("")
            // atomically move to safe place while processing
            let lua = @"
local result = redis.call('RPOP', KEYS[1])
if result ~= nil
    redis.call('HSET', KEYS[2], ARGV[1], result)
end
return result"
            let pipelineId = Guid.NewGuid().ToString("N")
            let! message = !!redis.EvalAsync<'T * string>(lua, [| inboxKey; pipelineKey |], [| pipelineId |])
            if Object.Equals(message, null) then 
                let! recd = Async.AwaitWaitHandle(awaitMessageHandle, timeout)
                if recd then return! await timeout
                else return raise (TimeoutException("Receive timed out"))
            else return message, pipelineId
        }
    
    member __.Id = id
    member __.Children = children.Keys
    member __.QueueLength = int (redis.LLen(inboxKey))
    
    member __.ErrorHandler 
        with get () = errorHandler
        and set (eh) = errorHandler <- eh
    
    member private __.Computation = computation

    member __.Start() : unit = 
        if computation.IsNone then failwith "Cannot start an actor without computation"
        cts <- new CancellationTokenSource()
        async { 
            while not cts.Token.IsCancellationRequested do
                do! !~semaphor.WaitAsync(cts.Token)
                let! msg = await Timeout.Infinite
                let payload = (fst (fst msg))
                let messageId = (snd (fst msg))
                let pipelineId = snd msg
                async { 
                    try 
                        let! result = computation.Value payload
                        children.Values |> Seq.iter (fun a -> a.Post(result))
                        redis.HDelAsync(pipelineKey, pipelineId) |> ignore
                        if messageId <> "" then 
                            redis.HSet(resultsKey, messageId, result, When.Always, true) |> ignore
                            redis.PublishAsync<string>(channelKey, messageId) |> ignore
                    with e -> 
                        let ei = ExceptionInfo(id, payload, e)
                        redis.LPush<ExceptionInfo<'Tin>>(errorsKey, ei, When.Always, true) |> ignore
                        if errorHandler <> Unchecked.defaultof<Actor<ExceptionInfo<'Tin>, _>> then
                            errorHandler.Post(ei)
                }
                |> Async.Start
                semaphor.Release() |> ignore
        }
        |> Async.Start
        started <- true
    
    member __.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
    
    member __.Post(message : 'Tin, ?highPriority:bool) : unit = 
        let highPriority = defaultArg highPriority false
        if highPriority then redis.LPushAsync<'Tin * string>(inboxKey, (message, "")) |> ignore
        else redis.RPushAsync<'Tin * string>(inboxKey, (message, "")) |> ignore
        awaitMessageHandle.Set() |> ignore
        redis.PublishAsync<string>(channelKey, "") |> ignore
    
    member __.PostAndReply(message : 'Tin, ?highPriority:bool, ?millisecondsTimeout) : Async<'Tout> = 
        let highPriority = defaultArg highPriority false
        let millisecondsTimeout = defaultArg millisecondsTimeout Timeout.Infinite
        match started with
        | true -> computation.Value message
        | false -> 
            let resultId = Guid.NewGuid().ToString("N")
            if highPriority then redis.LPushAsync<'Tin * string>(inboxKey, (message, resultId)) |> ignore
            else redis.RPushAsync<'Tin * string>(inboxKey, (message, resultId)) |> ignore
            awaitMessageHandle.Set() |> ignore
            redis.PublishAsync<string>(channelKey, "") |> ignore // no resultId here because we notify recievers that in turn will notify callers about results
            let rec awaitResult timeout = 
                async { 
                    let! message = !!redis.HGetAsync<'Tout>(resultsKey, resultId)
                    if Object.Equals(message, null) then 
                        let! recd = Async.AwaitWaitHandle(awaitResultHandle, timeout)
                        if recd then return! awaitResult timeout
                        else return raise (TimeoutException("PostAndReply timed out"))
                    else return message
                }
            awaitResult millisecondsTimeout
    
    member __.Link(actor : Actor<'Tout, _>) : unit = children.Add(actor.Id, actor)
    member __.UnLink(actor : Actor<'Tout, _>) : bool = children.Remove(actor.Id)
    

    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            awaitMessageHandle.Dispose()
            awaitResultHandle.Dispose()
            cts.Dispose()
            semaphor.Dispose() 