namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("Fredis")>]
[<assembly: AssemblyProductAttribute("Fredis")>]
[<assembly: AssemblyDescriptionAttribute("Fredis (F#+Redis) is dead simple collection of APIs for POCOs cache/persistence + redis based distributed actors")>]
[<assembly: AssemblyVersionAttribute("0.0.9")>]
[<assembly: AssemblyFileVersionAttribute("0.0.9")>]

[<assembly: InternalsVisibleToAttribute("Fredis.CS.Tests")>]
[<assembly: InternalsVisibleToAttribute("Fredis.Tests")>]
[<assembly: InternalsVisibleToAttribute("Fredis.Profiler")>]

do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.9"
