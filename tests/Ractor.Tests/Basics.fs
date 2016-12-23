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
    override this.ResultTimeout = 50000 // milliseconds

type Echo () =
    inherit Actor<string,string>()
    override this.RedisConnectionString = "127.0.0.1:6379,resolveDns=true"
    override this.Computation(input) : Task<string> = 
        task { 
            Console.WriteLine("Echo: " + input)
            return input 
        } 
    //override this.AutoStart = true // true is default, no need to override

type Incrementer ()=
    inherit Actor<int,Tuple<int>>()
    override this.RedisConnectionString = "127.0.0.1:6379,resolveDns=true"
    override this.Computation(input) : Task<Tuple<int>> =  
        task {
                //Console.WriteLine("Incremented to: " + (input + 1).ToString())
                return Tuple.Create(input + 1)
        }
    override this.AutoStart = false
    override this.ResultTimeout = 50000 // milliseconds


[<Test>]
let ``Hello, Actors`` () =
    
    let greeter = Greeter()

    // we could use new instance each time for shorter syntax
    
    Greeter().PostAsync("Greeter 0 (nice syntax)") |> ignore

    let sameGreeter = Greeter()
    greeter.PostAsync("Greeter 1") |> ignore
    greeter.PostAsync("Greeter 2") |> ignore
    greeter.PostAsync("Greeter 3") |> ignore
    greeter.PostAsync("Greeter 4") |> ignore
    greeter.PostAsync("Greeter 5") |> ignore

    Console.WriteLine("Not started yet")
    Thread.Sleep(1000)
    greeter.Start()
    Thread.Sleep(1000)
    sameGreeter.PostAsync("Greeter via sameGreeter, started") |> ignore
    
    greeter.PostAsync("Greeter 6") |> ignore

    Thread.Sleep(10000)

    greeter.Stop()
    ()



[<Test>]
let ``Iterate`` () =

    let incr = Incrementer()

    incr.Start()

    let limit = 50

    let sw = Stopwatch.StartNew()
    
    let mutable value = 0

//    let r = Redis("localhost")
//    r.Set("testIncr", 0)
    for i in 1..limit do
        //r.Set("testIncr", value + 1)
        value <- incr.PostAndGetResultAsync(value).Result.Item1 // (int (r.Get<int>("testIncr"))) //
    sw.Stop()

    Console.WriteLine("Final value: " + (value).ToString())
    Console.WriteLine("Elapsed msec: " + sw.ElapsedMilliseconds.ToString())
    
    //Thread.Sleep(5000)
    
    ()
