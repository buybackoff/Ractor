namespace Fredis

open System
open System.Linq
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open Fredis

// TODO exception handling
// TODO A system actor that periodically checks pipelines and errors of all actors
// and is subscribed to errors channel as well

type internal ExceptionInfo<'T> = 
    | ExceptionInfo of string * 'T * Exception


type Actor<'Tin, 'Tout> internal (redis : Redis, id : string, computation : 'Tin -> Async<'Tout>) = 
    // linking only works on children with computations returning unit
    let children = Dictionary<string, Actor<'Tout, unit>>()
    let mutable started = false
    let mutable cts = Unchecked.defaultof<CancellationTokenSource>
    let awaitMessageHandle = new AutoResetEvent(false)
    let awaitResultHandle = new AutoResetEvent(false)
    let prefix = id + ":Mailbox"
    let inboxKey = prefix + ":inbox"
    let pipelineKey = prefix + ":pipeline"
    let resultsKey = prefix + ":results"
    let channelKey = prefix + ":channel"
    let errorsKey = prefix + ":errors"
    let mutable errorHandler = Unchecked.defaultof<Actor<ExceptionInfo<'Tin>, _>>
    // global limit for number of concurrent tasks
    static let semaphor = new SemaphoreSlim(Environment.ProcessorCount * 16)
    
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
if result ~= nil then
    redis.call('HSET', KEYS[2], KEYS[3], result)
end
return result"
            // TODO add ZSet with timestamp as rank to monitor the pipeline state
            let pipelineId = Guid.NewGuid().ToString("N")
            
            let! message = 
                redis.EvalAsync<'T * string>(lua, 
                                        [| redis.KeyNameSpace + ":" + inboxKey
                                           redis.KeyNameSpace + ":" + pipelineKey
                                           pipelineId |]) |> Async.AwaitTask
            if Object.Equals(message, null) then 
                let! recd = Async.AwaitWaitHandle(awaitMessageHandle, timeout)
                if recd then return! await timeout
                else return raise (TimeoutException("Receive timed out"))
            else return message, pipelineId
        }
    
    member this.Id = id
    member this.Children = children.Keys.ToArray()
    member this.QueueLength = int (redis.LLen(inboxKey))
    
    // public error handling should be C# friendly
    member internal this.ErrorHandler 
        with get () = errorHandler
        and set (eh) = errorHandler <- eh
    
    member private this.Computation = computation
    
    member this.Start() : unit = 
        cts <- new CancellationTokenSource()
        async { 
            while (not cts.Token.IsCancellationRequested) do
                do! semaphor.WaitAsync(cts.Token)
                    |> Async.AwaitIAsyncResult
                    |> Async.Ignore
                try 
                    let! msg = await Timeout.Infinite
                    let payload = (fst (fst msg))
                    let messageId = (snd (fst msg))
                    let pipelineId = snd msg
                    async { 
                        try 
                            let! result = computation payload
                            children |> Seq.iter (fun a -> a.Value.Post(result))
                            redis.HDel(pipelineKey, pipelineId, true) |> ignore
                            if messageId <> "" then 
                                redis.HSet(resultsKey, messageId, result, When.Always, true) |> ignore
                                redis.Publish<string>(channelKey, messageId, true) |> ignore
                        with e -> 
                            let ei = ExceptionInfo(id, payload, e)
                            redis.LPush<ExceptionInfo<'Tin>>(errorsKey, ei, When.Always, true) |> ignore
                            if errorHandler <> Unchecked.defaultof<Actor<ExceptionInfo<'Tin>, _>> then 
                                errorHandler.Post(ei)
                    }
                    |> Async.Start
                finally
                    semaphor.Release() |> ignore
        }
        |> Async.Start
        started <- true
    
    member this.Stop() = 
        if started then 
            started <- false
            cts.Cancel |> ignore
    

    member this.Post<'Tin>(message : 'Tin) : unit =
        this.Post<'Tin>(message, false)

    member this.Post<'Tin>(message : 'Tin, highPriority : bool) : unit = 
        // TODO? local execution if started? similar to PostAndReply.
        // that will be good for chained actors - will save one trip to Redis and will have data to process right now, while pipeline is fire-and-forget write
        if highPriority then redis.RPushAsync<'Tin * string>(inboxKey, (message, "")) |> ignore
        else redis.LPushAsync<'Tin * string>(inboxKey, (message, "")) |> ignore
        awaitMessageHandle.Set() |> ignore
        redis.Publish<string>(channelKey, "", true) |> ignore
    
    member internal this.PostAndReply(message : 'Tin, highPriority : bool, millisecondsTimeout) : Async<'Tout> = 
        match started with
        | true -> 
            let pipelineId = Guid.NewGuid().ToString("N")
            redis.HSet<'Tin>(pipelineKey, pipelineId, message, When.Always, true) |> ignore // save message
            async { 
                let! result = computation message
                children |> Seq.iter (fun a -> a.Value.Post(result))
                redis.HDel(pipelineKey, pipelineId, true) |> ignore
                return result
            }
        | false ->  
            let resultId = Guid.NewGuid().ToString("N")
            if highPriority then redis.RPushAsync<'Tin * string>(inboxKey, (message, resultId)) |> ignore
            else redis.LPushAsync<'Tin * string>(inboxKey, (message, resultId)) |> ignore
            awaitMessageHandle.Set() |> ignore
            redis.Publish<string>(channelKey, "", true) |> ignore // no resultId here because we notify recievers that in turn will notify callers about results
            let rec awaitResult timeout = 
                async { 
                    let! message = redis.HGetAsync<'Tout>(resultsKey, resultId) |> Async.AwaitTask
                    if Object.Equals(message, null) then 
                        let! recd = Async.AwaitWaitHandle(awaitResultHandle, timeout)
                        if recd then return! awaitResult timeout
                        else return raise (TimeoutException("PostAndReply timed out"))
                    else 
                        redis.HDel(resultsKey, resultId, true) |> ignore
                        return message
                }
            awaitResult millisecondsTimeout
    
    
    member this.PostAndReplyAsync(message : 'Tin) : Task<'Tout> = 
        this.PostAndReplyAsync(message, false, Timeout.Infinite)
    
    member this.PostAndReplyAsync(message : 'Tin, highPriority : bool) : Task<'Tout> = 
        this.PostAndReplyAsync(message, highPriority, Timeout.Infinite)

    member this.PostAndReplyAsync(message : 'Tin, millisecondsTimeout) : Task<'Tout> = 
        this.PostAndReplyAsync(message, false, millisecondsTimeout)

    // C# naming style and return type
    member this.PostAndReplyAsync(message : 'Tin, highPriority : bool, millisecondsTimeout) : Task<'Tout> = 
        let res : Async<'Tout> = 
            this.PostAndReply(message, highPriority, millisecondsTimeout)
        res |> Async.StartAsTask
    
    member internal this.Link(actor : Actor<'Tout, unit>) = 
        children.Add(actor.Id, actor)
        this
    
    member internal this.Link(actors : IEnumerable<Actor<'Tout, unit>>) = 
        Seq.iter (fun (a : Actor<'Tout, _>) -> children.Add(a.Id, a)) actors
        this
    
    member internal this.UnLink(actor : Actor<'Tout, unit>) : bool = children.Remove(actor.Id)
    interface IDisposable with
        member x.Dispose() = 
            cts.Cancel |> ignore
            awaitMessageHandle.Dispose()
            awaitResultHandle.Dispose()
            cts.Dispose()
            semaphor.Dispose()
