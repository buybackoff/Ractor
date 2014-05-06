module Fredis.Tests

open System
open System.Linq
open System.Text
open System.Threading.Tasks
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

    sameGreeter.Post("Greeter via instance from Fredis.GetActor")

    // this will fail if computation returns not Async<unit>
    "greeter" <-- "Greeter via operator"

    Console.WriteLine("Not started yet")
    Thread.Sleep(1000)
    greeter.Start()
    Thread.Sleep(1000)
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
    Console.WriteLine(greeter.PostAndReply("Greeter 1") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndReply("Greeter 2") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndReply("Greeter 3") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndReply("Greeter 4") |> Async.RunSynchronously)
    Console.WriteLine(greeter.PostAndReply("Greeter 5") |> Async.RunSynchronously)

    Console.WriteLine(sameGreeter.PostAndReply("Greeter via instance from Fredis.GetActor") |> Async.RunSynchronously)

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

    let actorWithContinuation = first.ContinueWith(second)
    let actorWithTwoContinuations = first.ContinueWith(second, third)

    Console.WriteLine("Remote execution")
    //actorWithContinuation.Start()

    // type annotations are required
    let res = actorWithContinuation.PostAndReply("Continuation test", 1000) |> Async.RunSynchronously
    let res2 = actorWithTwoContinuations.PostAndReply("Continuation test 2", 1000) |> Async.RunSynchronously

    Console.WriteLine(res)
    Console.WriteLine(fst res2 + ":" + snd res2)
    ()