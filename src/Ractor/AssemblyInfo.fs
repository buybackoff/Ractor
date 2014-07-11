namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("Ractor")>]
[<assembly: AssemblyProductAttribute("Ractor")>]
[<assembly: AssemblyDescriptionAttribute("Ractor (Redis Actor, also see The Diamond Age by Neal Stephenson) is 
a distributed actor system with CLR/JVM interop and dead simple API for POCOs cache/persistence")>]
[<assembly: AssemblyVersionAttribute("0.1.1")>]
[<assembly: AssemblyFileVersionAttribute("0.1.1")>]
[<assembly: InternalsVisibleToAttribute("Ractor.CS.Tests")>]
[<assembly: InternalsVisibleToAttribute("Ractor.Tests")>]
[<assembly: InternalsVisibleToAttribute("Ractor.Profiler")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1.1"
