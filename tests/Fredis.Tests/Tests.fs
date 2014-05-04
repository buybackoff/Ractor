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
    firstGreeter.Start()

    //firstGreeter.Post("First Greeter")
      
    Thread.Sleep(1000)
    //Console.WriteLine(t.Result)
    ()