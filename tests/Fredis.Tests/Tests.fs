module Fredis.Tests

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
open NUnit.Framework

[<Test>]
let ``hello, Fredis`` () =
    let fredis = new Fredis("localhost")

    let computation (input:string) : Async<unit> =
        async {
            Console.WriteLine("Hello, " + input)
        }

    let greeter = fredis.CreateActor("greeter", computation)
    // type annotations are required
    let sameGreeter  = Fredis.GetActor<string, unit>("greeter")
    greeter.Post("Greeter 1")
    greeter.Post("Greeter 2")
    greeter.Post("Greeter 3")
    greeter.Post("Greeter 4")
    greeter.Post("Greeter 5")

    Console.WriteLine("Not started yet")
    Thread.Sleep(1000)
    greeter.Start()

    sameGreeter.Post("Greeter via instance from Fredis.GetActor")
    // this will fail if computation returns not Async<unit>
    "greeter" <-- "Greeter via operator"
    
    Thread.Sleep(1000)
    ()

[<Test>]
let ``Iterate`` () =
    let fredis = new Fredis("localhost")

    let computation (input:string) : Async<string> =
        async {
            //do! Async.Sleep(1000)
            return ("Hello, " + input)
        }

    let greeter = fredis.CreateActor("greeter", computation)
    let greeter2 = fredis.CloneActor<string,string>("greeter")
    greeter.Start()
    greeter2.Start()

    let limit = 10000 / 2
    let guids = Array.init limit (fun _ -> Guid.NewGuid())
    let guids2 = Array.init limit (fun _ -> Guid.NewGuid())

    let sw = Stopwatch.StartNew()
    
    let t1 = guids |> Array.Parallel.map (fun g -> greeter.PostAndGetResult("PING")) 
                    |> Async.Parallel
    let t2 =  guids2 |> Array.Parallel.map (fun g -> greeter2.PostAndGetResult("PING")) 
                    |> Async.Parallel
    let res = [|t1; t2|] |> Async.Parallel |> Async.RunSynchronously |> Seq.map Seq.length |> Seq.sum
    //guids |> Array.Parallel.map (fun g -> greeter.Post("PING", g)) |> ignore

//    let tasks : List<Async<string>> = List<Async<string>>()
//    for i in 1..50000 do
//        tasks.Add(greeter.PostAndGetResult("PING"))
//    let res = tasks |> Async.Parallel |> Async.RunSynchronously

    sw.Stop()

    Console.WriteLine("Received count: " + (res).ToString())
    Console.WriteLine("Elapsed ms: " + sw.ElapsedMilliseconds.ToString())
    Thread.Sleep(5000)
    
    ()

[<Test>]
let ``PostAndReply local execution`` () =
    let fredis = new Fredis("localhost")

    let computation (input:string) : Async<string> =
        async {
            return ("Hello, " + input)
        }

    

    let greeter = fredis.CreateActor("greeterReply", computation)
    
    greeter.Start()

    // type annotations are required
    let sameGreeter  = Fredis.GetActor<string, string>("greeterReply")
    Console.WriteLine(greeter.PostAndGetResult("Greeter 1") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndGetResult("Greeter 2") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndGetResult("Greeter 3") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndGetResult("Greeter 4") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndGetResult("Greeter 5") |> Async.RunSynchronously)

    Console.WriteLine(sameGreeter.PostAndGetResult("Greeter via instance from Fredis.GetActor") |> Async.RunSynchronously)

    // this will fail if computation returns not Async<unit>
    let res : string = "greeterReply" <-* "Greeter via operator"  |> Async.RunSynchronously
    Console.WriteLine(res)
    ()


[<Test>]
let ``Continuation basic`` () =
     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
    
    let fredis = new Fredis("localhost")

    let computation (input:string) : Async<string> =
        async {
            return ("Hello, " + input)
        }

    let computation2 (input:string) : Async<string> =
        async {
            return (input + "; this is continuation2" )
        }

    let computation3 (input:string) : Async<string> =
        async {
            return (input + "; this is continuation3" )
        }

    let first = fredis.CreateActor("first", computation)
    let second = fredis.CreateActor("second", computation2)
    let third = fredis.CreateActor("third", computation3)
    first.Start()
    second.Start()
    third.Start()

    let actorWithContinuation = first.ContinueWith(second).ContinueWith(third)
    //let actorWithTwoContinuations = first.ContinueWith(second, third)

    actorWithContinuation.Start()

    // type annotations are required
    let res = actorWithContinuation.PostAndGetResult("Continuation test", 5000) |> Async.RunSynchronously
    //let res2 = actorWithTwoContinuations.PostAndGetResult("Continuation test 2", 1000) |> Async.RunSynchronously

    Console.WriteLine(res)
    //Console.WriteLine(fst res2 + ":" + snd res2)

    Thread.Sleep(1000)

    ()