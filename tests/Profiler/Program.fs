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

    let limit = 100000 / 2
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
    
    Console.ReadLine() |> ignore
    
    0
