module Fredis.Tests

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open Fredis.FSharp
open System
open System.Text
open System.Diagnostics
open System.Threading
open NUnit.Framework
open FsUnit

type Greeter ()=
    inherit Actor<string,string>()
    override this.Redis = "localhost"
    override this.Computation(input) : Async<string> =  
        async {
                Console.WriteLine("Hello, " + input)
                return "Hello, " + input
        }
    override this.AutoStart = false
    override this.ResultTimeout = 500 // milliseconds

// C# friendly actor
type Echo () =
    inherit Fredis.Actor<string,string>()
    override this.Redis = "localhost"
    override this.Computation(input) : Task<string> = async { return input } |> Async.StartAsTask
    //override this.AutoStart = true // true is default, no need to override

[<Test>]
let ``Hello, Actors`` () =
    
    let fredis = new Fredis("localhost")

    let greeter = Greeter()

    // we could use new instance each time for shorter syntax
    // it would be nice to have static members, but they don't work with inheritance
    // garbage from this is very small, and it is unusual to use the same actor
    // many times in one place other than in loops - BEWARE OF LOOPS AND USE INSTANCE!
    // usually each actor is used inside a service call once
    // but even in a loop that wouldn't be the biggest problem since Actor<> def is
    // very lightweight ans stateless: class overhead + 2 refs + 1 int + 3 bools
    Greeter().Post("Greeter 0 (nice syntax)") |> ignore

    let sameGreeter = Greeter()
    greeter.Post("Greeter 1") |> ignore
    greeter.Post("Greeter 2") |> ignore
    greeter.Post("Greeter 3") |> ignore
    greeter.Post("Greeter 4") |> ignore
    greeter.Post("Greeter 5") |> ignore

    Console.WriteLine("Not started yet")
    Thread.Sleep(3000)
    greeter.Start()
    Thread.Sleep(3000)
    sameGreeter.Post("Greeter via sameGreeter, started") |> ignore
    
    Thread.Sleep(1000)
    greeter.Post("Greeter 6") |> ignore

    // doesn't work correctly yet
    greeter.ContinueWith(greeter).Post("Double greeter") |> ignore

    Thread.Sleep(1000)

    greeter.Stop()
    ()








//
//
//
//
//[<Test>]
//let ``Iterate`` () =
//
//    let echo = Echo()
//    let echo2 = Echo()
//    echo.Start()
//    echo2.Start()
//
//    let limit = 10000 / 2
//    let guids = Array.init limit (fun _ -> Guid.NewGuid())
//    let guids2 = Array.init limit (fun _ -> Guid.NewGuid())
//
//    let sw = Stopwatch.StartNew()
//    
////    for i in 0..(limit*2) do
////        greeter.PostAndGetResult("PING") |> Async.RunSynchronously |> ignore
//
////    let t1 = guids |> Array.Parallel.map (fun g -> echo.PostAndGetResult("PING")) 
////                    |> Async.Parallel
////    let t2 =  guids2 |> Array.Parallel.map (fun g -> echo2.PostAndGetResult("PING")) 
////                    |> Async.Parallel
////    let res = [|t1; t2|] |> Async.Parallel |> Async.RunSynchronously |> Seq.map Seq.length |> Seq.sum
//    //guids |> Array.Parallel.map (fun g -> greeter.Post("PING", g)) |> ignore
//
////    let tasks : List<Async<string>> = List<Async<string>>()
////    for i in 1..50000 do
////        tasks.Add(greeter.PostAndGetResult("PING"))
////    let res = tasks |> Async.Parallel |> Async.RunSynchronously
//
//    sw.Stop()
//
//    //Console.WriteLine("Received count: " + (res).ToString())
//    Console.WriteLine("Elapsed ms: " + sw.ElapsedMilliseconds.ToString())
//    Thread.Sleep(5000)
//    
//    ()
//
//[<Test>]
//let ``PostAndReply local execution`` () =
//    let fredis = new Fredis("localhost")
//
////    let computation (input:string) : Async<string> =
////        async {
////            return ("Hello, " + input)
////        }
////
////    
////
////    let greeter = fredis.CreateActor("greeterReply", computation)
////    
////    greeter.Start()
////
////    // type annotations are required
////    let sameGreeter  = Fredis.GetActor<string, string>("greeterReply")
////    Console.WriteLine(greeter.PostAndGetResult("Greeter 1") |> Async.RunSynchronously)
////    Console.WriteLine(greeter.PostAndGetResult("Greeter 2") |> Async.RunSynchronously)
////    Console.WriteLine(greeter.PostAndGetResult("Greeter 3") |> Async.RunSynchronously)
////    Console.WriteLine(greeter.PostAndGetResult("Greeter 4") |> Async.RunSynchronously)
////    Console.WriteLine(greeter.PostAndGetResult("Greeter 5") |> Async.RunSynchronously)
////
////    Console.WriteLine(sameGreeter.PostAndGetResult("Greeter via instance from Fredis.GetActor") |> Async.RunSynchronously)
////
////    // this will fail if computation returns not Async<unit>
////    let res : string = "greeterReply" <-* "Greeter via operator"  |> Async.RunSynchronously
////    Console.WriteLine(res)
//    ()
//
//
//[<Test>]
//let ``Continuation basic`` () =
//     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
//    
//    let fredis = new Fredis("localhost")
//
////    let computation (input:string) : Async<string> =
////        async {
////            return ("Hello, " + input)
////        }
////
////    let computation2 (input:string) : Async<string> =
////        async {
////            return (input + "; this is continuation2" )
////        }
////
////    let computation3 (input:string) : Async<string> =
////        async {
////            return (input + "; this is continuation3" )
////        }
////
////    let first = fredis.CreateActor("first", computation)
////    let second = fredis.CreateActor("second", computation2)
////    let third = fredis.CreateActor("third", computation3)
////    first.Start()
////    second.Start()
////    third.Start()
////
////    let actorWithContinuation = first.ContinueWith(second).ContinueWith(third)
////    //let actorWithTwoContinuations = first.ContinueWith(second, third)
////
////    actorWithContinuation.Start()
////
////    // type annotations are required
////    let res = actorWithContinuation.PostAndGetResult("Continuation test", 5000) |> Async.RunSynchronously
////    //let res2 = actorWithTwoContinuations.PostAndGetResult("Continuation test 2", 1000) |> Async.RunSynchronously
////
////    Console.WriteLine(res)
////    //Console.WriteLine(fst res2 + ":" + snd res2)
////
////    Thread.Sleep(1000)
//
//    ()