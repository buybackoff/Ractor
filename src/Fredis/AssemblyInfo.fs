namespace Fredis
open System
open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("Fredis")>]
[<assembly: AssemblyProductAttribute("Fredis")>]
[<assembly: AssemblyDescriptionAttribute("Fredis")>]
[<assembly: AssemblyVersionAttribute("0.0.4")>]
[<assembly: AssemblyFileVersionAttribute("0.0.4")>]
[<assembly: Guid ("2A3CA787-3968-4C4B-9617-8743753D0185")>]

do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.4"
