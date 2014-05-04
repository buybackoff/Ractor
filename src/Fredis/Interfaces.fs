namespace Fredis

open System
open System.Threading

[<AutoOpen>]
module Types = 
    
    type ExceptionInfo<'T> = 
        | ExceptionInfo of string * 'T * Exception
