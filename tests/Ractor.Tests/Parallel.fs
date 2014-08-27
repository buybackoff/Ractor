module Ractor.Tests.Parallel

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open System.Collections.Generic
open Ractor
open Ractor.FSharp
open System
open System.Text
open System.Diagnostics
open System.Threading
open NUnit.Framework
open FsUnit


type Slowpoke ()=
    inherit Actor<int,int>()
    override this.RedisConnectionString = "localhost"
    override this.Computation(input) : Async<int> =  
        async {
                //do! Async.Sleep(500)
                Console.WriteLine("Incremented to: " + (input + 1).ToString())
                return input + 1
        }
    override this.AutoStart = true
    override this.ResultTimeout = 50000 // milliseconds
    override this.Optimistic = false

[<Test>]
let ``Could run actors in parallel`` () =
     //TODO with static repo cannot create another actor in the same process. LOL, that was intended
    let slowpoke = Slowpoke()
    slowpoke.Start()
    for i in 1..10 do
        let sw = Stopwatch()
        sw.Start()
        let result = 
                slowpoke
                    .ParallelWith(slowpoke)
                    .ParallelWith(slowpoke)
                    .ParallelWith(slowpoke)
                    .PostAndGetResult(((1,1),1),1)
        
        result |> unnest4 |> (fun (v1, v2, v3, v4) -> v1 + v2 + v3 + v4) |> should equal 8
        sw.Stop()
        Console.WriteLine("Elapsed: " + sw.ElapsedMilliseconds.ToString())
    ()