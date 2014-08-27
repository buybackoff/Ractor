module Ractor.Tests.Continuations

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open Ractor.FSharp
open System
open System.Text
open System.Diagnostics
open System.Threading
open NUnit.Framework
open FsUnit


type Incrementer ()=
    inherit Actor<int,int>()
    override this.RedisConnectionString = "localhost"
    override this.Computation(input) : Async<int> =  
        async {
                //Console.WriteLine("Incremented to: " + (input + 1).ToString())
                return input + 1
        }
    override this.AutoStart = true
    override this.ResultTimeout = 50000 // milliseconds

type Adder ()=
    inherit Actor<int*int,int>()
    override this.RedisConnectionString = "localhost"
    override this.Computation((input1, input2)) : Async<int> =  
        async {
                Console.WriteLine("Added to: " + (input1 + input2).ToString())
                return input1 + input2
        }
    override this.AutoStart = true
    override this.ResultTimeout = 50000 // milliseconds

[<Test>]
let ``Could chain continuations and get result`` () =
     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
    let incrementer = Incrementer()
    incrementer.Start()

    
    let result = 
            incrementer
                .ContinueWith(incrementer)
                .ContinueWith(incrementer)
                .ContinueWith(incrementer)
                .PostAndGetResult(1)
    result |> should equal 5

    let r2 = (incrementer ->- incrementer ->- incrementer ->- incrementer).PostAndGetResult(1)
    r2 |> should equal 5
    ()


[<Test>]
let ``Could run parallels then continue`` () =
     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
    let incrementer = Incrementer()
    incrementer.Start()

    for i in 1..100 do
        let sw = Stopwatch()
        sw.Start()

        let result = 
                ((   (incrementer |^| incrementer) 
                ->- (incrementer |^| incrementer))
                ->- Adder())
                    .PostAndGetResult(1, 1)
        result |> should equal 6
        sw.Stop()
        Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds.ToString())
    ()

[<Test>]
let ``Could run continuations in parallel`` () =
     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
    let incrementer = Incrementer()
    incrementer.Start()

    let result1, result2 = 
            (   (incrementer 
                        ->- incrementer
                                ->- incrementer) 
            |^| (incrementer ->- incrementer))
                .PostAndGetResult(1, 1)
    result1+result2 |> should equal 7
    ()