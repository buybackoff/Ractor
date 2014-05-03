namespace Fredis

open System
open System.Threading

[<AutoOpen>]
module Types = 
    type ILogger = 
        abstract Debug : string * exn option -> unit
        abstract Info : string * exn option -> unit
        abstract Warning : string * exn option -> unit
        abstract Error : string * exn option -> unit
    
    type ExceptionInfo<'T> = 
        | ExceptionInfo of string * 'T * Exception


//    type IActor<'Tin,'Tout> = 
//         /// <summary>
//         /// Identificator (name) of actor
//         /// </summary>
//         abstract Id : string with get
//         abstract Post : 'Tin -> unit
//         abstract PostAndReply : 'Tin -> Async<'Tout>
//         abstract Link : IActor<'Tout,_> -> unit
//         abstract UnLink : IActor<'Tout,_> -> unit
//         abstract Children : seq<string> with get
//         abstract QueueLength : int with get
//         abstract Start : unit -> unit
//         abstract Stop : unit -> unit
