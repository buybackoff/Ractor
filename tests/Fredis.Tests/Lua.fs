module Lua

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
open NUnit.Framework
open FsUnit // NUnit.Framework


[<Test>]
let ``Fredis garbage collection`` () =
    let redis = new Redis("localhost", "test")
    redis.Del("hkey") |> ignore
    redis.Del("hkey:previousKeys") |> ignore
    let mutable count = 0
    // works on empty
    count <- Helpers.deleteRepeatingItemsInHSET(redis, "test:hkey")
    count |> should equal 0

    redis.HSet<string>("hkey", "field", "result") |> ignore
    count <- Helpers.deleteRepeatingItemsInHSET(redis, "test:hkey")
    count |> should equal 0 // first run
    count <- Helpers.deleteRepeatingItemsInHSET(redis, "test:hkey") 
    count |> should equal 1 // removed it
    count <- Helpers.deleteRepeatingItemsInHSET(redis, "test:hkey")
    count |> should equal 0 // hash is empty now
    // TODO 
    ()