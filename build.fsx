#I @"packages/FAKE/tools/"
#r @"FakeLib.dll"

open Fake 
open Fake.AssemblyInfoFile
open Fake.MSBuild

Target "AssemblyInfo" (fun _ ->
    let fileName = "FSharpWeb1/AssemblyInfo.fs"
    CreateFSharpAssemblyInfo fileName
        [ Attribute.Title "Demo"
          Attribute.Product "Demo"
          Attribute.Description "Demo of F# on the web"
          Attribute.Version "1.0.0"
          Attribute.FileVersion "1.0.0" ]
)

Target "RestorePackages" RestorePackages

Target "Clean" (fun _ ->
    CleanDirs ["bin"]
)

Target "Build" (fun _ -> 
    !! ("*/**/*.*proj")
    |> MSBuildRelease "bin" "Rebuild"
    |> Log "AppBuild-Output: "
)

Target "RunTests" (fun _ ->
    !! ("bin/*Test*.dll")
    |> NUnit (fun p -> 
        {p with 
            DisableShadowCopy = true
            TimeOut = System.TimeSpan.FromMinutes 20.
            OutputFile = "bin/TestResults.xml" })
)

Target "Release" DoNothing
Target "All" DoNothing

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "All"

RunTargetOrDefault "All"
