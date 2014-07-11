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

type Incrementer ()=
    inherit Actor<int,int>()
    override this.Redis = "localhost"
    override this.Computation(input) : Async<int> =  
        async {
                //Console.WriteLine("Incremented to: " + (input + 1).ToString())
                return input + 1
        }
    override this.Optimistic = false
    override this.AutoStart = true
    override this.ResultTimeout = 50000 // milliseconds


[<EntryPoint>]
let main argv = 

    let incr = Incrementer()

    let limit = 10000

    let sw = Stopwatch.StartNew()
    
    let mutable value = 0

    for i in 1..limit do
        value <- incr.PostAndGetResultAsync(value) |> Async.RunSynchronously

    sw.Stop()

    Console.WriteLine("Final value: " + (value).ToString())
    Console.WriteLine("Elapsed ms: " + sw.ElapsedMilliseconds.ToString())
    
    
    Console.ReadLine() |> ignore

    0
