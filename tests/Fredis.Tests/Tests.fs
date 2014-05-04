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
    let fredis = new Fredis("localhost:6379")

    let computation (input:string) : Async<string> =
        Console.WriteLine("Hello, " + input)
        async {
            return "Hello, " + input
        }

    let firstGreeter = fredis.CreateActor("greeter", computation)
    //let sameGreeter  = fredis.GetActor<string, string>("greeter")
    firstGreeter.Post("First Greeter 1")
    firstGreeter.Post("First Greeter 2")
    firstGreeter.Post("First Greeter 3")
    firstGreeter.Post("First Greeter 4")
    Thread.Sleep(1000)
    firstGreeter.Start()
    Thread.Sleep(1000)
    //Console.WriteLine(t.Result)
    ()