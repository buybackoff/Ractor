module Fredis.Tests

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open Fredis
open NUnit.Framework

[<Test>]
let ``hello, Fredis`` () =
    let conn = new Connection("127.0.0.1", minPoolSize = 2, maxPoolSize = 20)
    let lim = 100000
    for i in 1..lim do
        use c = +conn // little sense in a single thread with non-blocking ops
        let ping = !!!c.Server.Ping()
        if i % 100 = 0 then
            Console.Write(i.ToString() + ":")
            Console.Write(ping.ToString() + " ")
        if i % 1500 =0 then Console.WriteLine("")

    for i in 1..lim do
        let ping = !!!conn.Server.Ping()
        if i % 100 = 0 then
            Console.Write(i.ToString() + ":")
            Console.Write(ping.ToString() + " ")
        if i % 1500 =0 then Console.WriteLine("")

[<Test>]
let ``blocking`` () =
    let conn = new Connection("127.0.0.1", minPoolSize = 2, maxPoolSize = 10)
    let lim = 9
    let ts = System.Collections.Generic.Dictionary()
    for i in 1..lim do
         // little sense in a single thread with non-blocking ops
        //let res = c.Lists.BlockingRemoveFirstString(0, [|"nonex"|], 1).Result

        let t = Task.Factory.StartNew(Action(fun _ -> 
            use c = +conn
            let t = c.Lists.BlockingRemoveFirstString(0, [|"nonex"|], 1)
            let res = t.Result
            
            let w = if box res = null then 0 else 1
            Console.WriteLine(w.ToString() )
            ()
            )
            )
        ts.Add(i, t)
//        let a = async {
//            use c = +conn
//            let t = c.Lists.BlockingRemoveFirstString(0, [|"nonex"|], 5)
//            let res = t.Result
//            //Console.WriteLine(res.ToString() )
//            return if box res = null then 0 else 1
//        }
//        let r =  a |> Async.Ignore |> Async.Start
        //Console.WriteLine(i.ToString() )

    Task.WaitAll(ts.Values.ToArray())

    //System.Threading.Thread.Sleep(10000)
 