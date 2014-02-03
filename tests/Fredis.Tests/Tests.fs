module Fredis.Tests

open System
open System.Linq
open System.Text
open System.Threading.Tasks
open Fredis
open NUnit.Framework

[<Test>]
let ``hello, Fredis`` () =
    let conn = new Connection("127.0.0.1")
    let lim = 10000
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

 