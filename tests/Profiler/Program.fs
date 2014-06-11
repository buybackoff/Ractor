open System
open System.Linq
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open Fredis
open System
open System.Text
open System.Diagnostics
open System.Threading

[<EntryPoint>]
let main argv = 

    let hp =
        MailboxProcessor.Start(fun inbox ->
            let rec loop n =
                async { do printfn "n = %d, HP waiting..." n
                        do! Async.Sleep(100)
                        let! msg = inbox.Receive()
                        return! loop(msg) }
            loop 0)

    let lp =
        MailboxProcessor.Start(fun inbox ->
            let rec loop n =
                
                async { do printfn "n = %d, LP waiting..." n
                        do! Async.Sleep(100)
                        Thread.CurrentThread.Priority <- ThreadPriority.BelowNormal
                        let! msg = inbox.Receive()
                        return! loop(msg) }
            loop 0)

    for i in 1..100 do
        hp.Post i
        lp.Post i
    
    Console.ReadLine() |> ignore

    0
