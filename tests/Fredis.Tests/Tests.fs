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
let ``Pipeline is cleared after Post`` () =
    let r = new Redis("localhost")


    ()