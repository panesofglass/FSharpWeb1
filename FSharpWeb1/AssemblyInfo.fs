namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Demo")>]
[<assembly: AssemblyProductAttribute("Demo")>]
[<assembly: AssemblyDescriptionAttribute("Demo of F# on the web")>]
[<assembly: AssemblyVersionAttribute("1.0.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.0"
