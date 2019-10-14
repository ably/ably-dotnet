#r "paket:
nuget Fake.Core.Target 
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.XUnit2
nuget Fake.DotNet.ILMerge
//"
// include Fake modules, see Fake modules section

open Fake.Core
open Fake.IO.Globbing.Operators 
open Fake.DotNet
open Fake.IO
open Fake.DotNet.Testing
open Fake.DotNet.Testing.XUnit2


let sourceDir = __SOURCE_DIRECTORY__
let netstandardTestDir = "src/IO.Ably.Tests.DotNetCore20"
let xUnit2 = XUnit2.run

let NetStandardSolution = "src/IO.Ably.NetStandard.sln"
let NetFrameworkSolution = "src/IO.Ably.NetFramework.sln"
let buildDir = Path.combine sourceDir "build"
let testResultsDir = Path.combine buildDir "tests"
let packageDir = Path.combine buildDir "package"
let configuration = DotNet.Release

let mergeJsonNet path outputPath = 
  let target = Path.combine path "IO.Ably.dll"
  let out = Path.combine outputPath "IO.Ably.dll"

  ILMerge.run 
    { ILMerge.Params.Create() with DebugInfo = true
                                   TargetKind = ILMerge.TargetKind.Library
                                   Internalize = ILMerge.InternalizeTypes.Internalize
                                   Libraries = 
                                      Seq.concat 
                                        [
                                          !! (Path.combine path "Newtonsoft.Json.dll")
                                        ]
                                   AttributeFile = target } out target


// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log (sprintf "Current dir: %s" sourceDir)
  Trace.log " --- Removing build folder ---"
  Directory.delete(buildDir) 
  Directory.delete(packageDir) 

  Directory.ensure testResultsDir
  Directory.ensure packageDir
)

Target.create "NetStandard - Build" (fun _ ->
  DotNet.build (fun opts -> {
    opts with Configuration = configuration
  }) NetStandardSolution
)

Target.create "NetStandard - Unit Tests" (fun _ ->
    Directory.ensure testResultsDir
    Trace.log " --- Testing net core version --- "
    let project = !! ("src/IO.Ably.Tests.DotNetCore20/*.csproj") |> Seq.head
    DotNet.test (fun opts -> { opts with Configuration = configuration
                                         Filter = Some ("type!=integration")
                                         Logger = Some( "trx;logfilename=" + (Path.combine testResultsDir "unit-tests-standard.trx"))})
                project
)

Target.create "NetFramework - Build" (fun _ ->
  let buildMode = Environment.environVarOrDefault "buildMode" "Release"
  let setParams (defaults:MSBuildParams) =
        { defaults with
            Verbosity = Some(Quiet)
            Targets = ["Build"]
            Properties =
                [
                    "Optimize", "True"
                    "DebugSymbols", "True"
                    "Configuration", buildMode
                ]
         }
  MSBuild.build setParams NetFrameworkSolution

  let buildPath = Path.combine "src/IO.Ably.NETFramework/bin" "Release"
  mergeJsonNet buildPath packageDir
)

Target.create "NetFramework - Unit Tests" (fun _ ->
    Directory.ensure testResultsDir
    Trace.log " --- Testing net core version --- "
    let project = !! ("src/IO.Ably.Tests.NETFramework/*.csproj") |> Seq.head
    DotNet.test (fun opts -> { opts with Configuration = configuration
                                         Filter = Some ("type!=integration")
                                         Logger = Some( "trx;logfilename=" + (Path.combine testResultsDir "unit-tests-framework.trx"))})
                project
)