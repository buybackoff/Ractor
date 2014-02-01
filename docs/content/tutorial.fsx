(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Hello World!
========================

TDB
*)

#r "BookSleeve.dll"
#r "Fredis.dll"

open System
open System.Text

open Fredis

let connection = new Connection("localhost")
let anotherReusedConnection = +connection
let subscriberChannel = %connection
let r1 = !!!connection.Server.Ping()
!~!connection.Strings.Set(2,"k1","1")
let a' = 
    async{
        // Async.AwaitIAsyncResult >> Async.Ignore
        return! !~connection.Strings.Set(1,"k1","abc")
    } |> Async.StartImmediate
// !~! shortcut for (Async.AwaitIAsyncResult >> Async.Ignore >> Async.StartImmediate)
let a = !~!connection.Strings.Set(1,"k1","abc") 
let r2 = async { return! !!connection.Strings.Append(1,"k1","def") } |> Async.RunSynchronously
let r3 = !!!connection.Strings.GetString(1,"k1")

(**
Some more info
*)
