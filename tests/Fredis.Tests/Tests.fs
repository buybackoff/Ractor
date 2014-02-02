module Fredis.Tests

open Fredis
open System
open System.Text
open System.Diagnostics
open NUnit.Framework

[<Test>]
let ``Could increment`` () =
    let conn = (new Connection("127.0.0.1", maxPoolSize = 2))
    let incTest c  = Async.StartImmediate (async {
        use! lock = makeLock c 0 "l" 60

        let! v1 = !!c.Strings.GetInt64(0, "testinc")
        let v2 = (if v1.HasValue then v1.Value else 0L) + 1L
        do! !~c.Strings.Set(0,"testinc",v2)
    })

    !~!conn.Strings.Set(0,"testinc",0L)
    incTest(conn)
    let countPerThread = 10
    let threadCount = 1
    !~!conn.Strings.Set(0,"testinc",0L)

    let incrementers () =
        Array.Parallel.init threadCount (fun _ ->
        let c = +conn
        for i = 1 to countPerThread do incTest(c))

    let res = !!!conn.Strings.GetInt64(0, "testinc")

    incrementers () |> ignore
    //if res.Value <> int64(countPerThread * threadCount) then failwith "you are stupid bastartd!" else Console.WriteLine("you are genius!")

    //res.Value
    ()