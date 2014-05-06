namespace Fredis

open System

type IFredisPerformanceMonitor =
    abstract AllowLowPriorityActors : unit -> bool
    abstract FrequencySeconds : int with get


type internal ExceptionInfo<'T> = 
    | ExceptionInfo of string * 'T * Exception