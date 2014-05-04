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

    let computation (input:string) : Async<unit> =
        async {
            Console.WriteLine("Hello, " + input)
        }

    let firstGreeter = fredis.CreateActor("greeter", computation)
    let sameGreeter  = Fredis.GetActor<string, unit>("greeter")
    firstGreeter.Post("First Greeter 1")
    firstGreeter.Post("First Greeter 2")
    firstGreeter.Post("First Greeter 3")
    firstGreeter.Post("First Greeter 4")
    firstGreeter.Post("First Greeter 5")
    sameGreeter.Post("First Greeter via same greeter")
    "greeter" <-- "First Greeter via operator"
    Console.WriteLine("Not started yet")
    Thread.Sleep(1000)
    firstGreeter.Start()
    Thread.Sleep(1000)
    ()