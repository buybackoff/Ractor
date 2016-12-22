module Ractor.Tests.Basics

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open Ractor
open System
open System.Text
open System.Diagnostics
open System.Threading
open NUnit.Framework
open FsUnit

type Greeter ()=
    inherit Actor<string,string>()
    override this.RedisConnectionString = "127.0.0.1:6379,resolveDns=true"
    override this.Computation(input) : Task<string> =  
        task {
                Console.WriteLine("Hello, " + input)
                return "Hello, " + input
        }
    override this.AutoStart = false
    override this.Optimistic = false
    override this.ResultTimeout = 50000 // milliseconds

type Echo () =
    inherit Actor<string,string>()
    override this.RedisConnectionString = "127.0.0.1:6379,resolveDns=true"
    override this.Computation(input) : Task<string> = 
        task { 
            Console.WriteLine("Echo: " + input)
            return input 
            } 
    override this.Optimistic = false
    //override this.AutoStart = true // true is default, no need to override

type Incrementer ()=
    inherit Actor<int,int>()
    override this.RedisConnectionString = "127.0.0.1:6379,resolveDns=true"
    override this.Computation(input) : Task<int> =  
        task {
                //Console.WriteLine("Incremented to: " + (input + 1).ToString())
                return input + 1
        }
    override this.Optimistic = true
    override this.AutoStart = true
    override this.ResultTimeout = 50000 // milliseconds


[<Test>]
let ``Hello, Actors`` () =
    
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
    Thread.Sleep(1000)
    greeter.Start()
    Thread.Sleep(1000)
    sameGreeter.Post("Greeter via sameGreeter, started") |> ignore
    
    greeter.Post("Greeter 6") |> ignore

    // doesn't work correctly yet
    greeter.ContinueWith(greeter).ContinueWith(greeter).ContinueWith(greeter)
            .Post("Echo greeter") |> ignore

    Thread.Sleep(10000)

    greeter.Stop()
    ()



[<Test>]
let ``Iterate`` () =

    let incr = Incrementer()

    let limit = 50000

    let sw = Stopwatch.StartNew()
    
    let mutable value = 0

//    let r = Redis("localhost")
//    r.Set("testIncr", 0)
    for i in 1..limit do
        //r.Set("testIncr", value + 1)
        value <- incr.PostAndGetResult(value) // (int (r.Get<int>("testIncr"))) //
    sw.Stop()

    Console.WriteLine("Final value: " + (value).ToString())
    Console.WriteLine("Elapsed msec: " + sw.ElapsedMilliseconds.ToString())
    
    //Thread.Sleep(5000)
    
    ()

[<Test>]
let ``PostAndReply local execution`` () =
    
    let greeter = Greeter()
    
    greeter.Start()

    // type annotations are required
    Console.WriteLine("Reply: " + greeter.PostAndGetResult("Greeter 1") )


//
//
//[<Test>]
//let ``Continuation basic`` () =
//     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
//    
//    let fredis = new Ractor("localhost")
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