#r "paket:
nuget Fake.Core.Target 
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.Testing.XUnit2
//"
// include Fake modules, see Fake modules section

open Fake.Core
open Fake.IO.Globbing.Operators 
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.DotNet.Testing.XUnit2

let netstandardTestDir = "src/IO.Ably.Tests.DotNetCore20"
let xUnit2 = XUnit2.run

let NetStandardSolution = "src/IO.Ably.NetStandard.sln"

// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log " --- Cleaning stuff --- "
)

Target.create "BuildNetStandard" (fun _ ->
  DotNet.build (fun opts -> {
    opts with Configuration = DotNet.Debug
  }) NetStandardSolution
)

Target.create "Unit Tests" (fun _ ->
    Trace.log " --- Testing net core version --- "
    !! "src/IO.Ably.Tests.DotNetCore20/*/*.DotNetCore20.dll"
    |> xUnit2 (fun p -> {
      p with Parallel = Collections
             XmlOutputPath = Some("/build/tests/unit.test.xml")
    })
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "BuildNetStandard"
  ==> "Unit Tests"

// *** Start Build ***
Target.runOrDefault "Unit Tests"