namespace System
open System.Reflection
open System.Runtime.CompilerServices

[<assembly: AssemblyTitleAttribute("Ractor")>]
[<assembly: AssemblyProductAttribute("Ractor")>]
[<assembly: AssemblyDescriptionAttribute("Redis based distributed actors + Dead simple API for distributed POCOs persistence")>]
[<assembly: AssemblyVersionAttribute("0.2.2")>]
[<assembly: AssemblyFileVersionAttribute("0.2.2")>]
[<assembly: InternalsVisibleToAttribute("Ractor.CS.Tests")>]
[<assembly: InternalsVisibleToAttribute("Ractor.Tests")>]
[<assembly: InternalsVisibleToAttribute("Ractor.Profiler")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.2"
