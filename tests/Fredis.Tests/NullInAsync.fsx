
open System
open System.Threading.Tasks
open System.Linq.Expressions

let nullable = Unchecked.defaultof<string * string>

let taskWithNull() =
    let tcs = new TaskCompletionSource<string * string>()
    tcs.SetResult(nullable)
    tcs.Task

let awaitResult = taskWithNull().Result


let asyncResult = 
    async {
        let! res = taskWithNull() |> Async.AwaitTask
        return res
    } |> Async.RunSynchronously