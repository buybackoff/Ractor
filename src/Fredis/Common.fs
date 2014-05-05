namespace Fredis



type IFredisPerformanceMonitor =
    abstract AllowLowPriorityActors : unit -> bool
    abstract FrequencySeconds : int with get

