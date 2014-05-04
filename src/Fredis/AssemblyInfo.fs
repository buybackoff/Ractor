namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Fredis")>]
[<assembly: AssemblyProductAttribute("Fredis")>]
[<assembly: AssemblyDescriptionAttribute("Fredis (F#+Redis) is dead simple collection of APIs for POCOs cache/persistence + redis based distributed actors")>]
[<assembly: AssemblyVersionAttribute("0.0.7")>]
[<assembly: AssemblyFileVersionAttribute("0.0.7")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.7"
