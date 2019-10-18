#r "paket:
nuget Fake.Core.Target 
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.XUnit2
nuget Fake.DotNet.ILMerge
//"

#r "System.Core.dll"
#r "System.Xml.Linq.dll"
// include Fake modules, see Fake modules section

open Fake.Core.TargetOperators
open Fake.Core.Operators
open Fake.Core
open Fake.IO.Globbing.Operators 
open Fake.DotNet
open Fake.IO
open Fake
open System
open Fake.DotNet.Testing
open Fake.DotNet.Testing.XUnit2
open System.Xml.Linq
open System.Xml.XPath
open FSharp.Core
open Fake.Testing.Common


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

let nugetRestore solutionFile = 
  CreateProcess.fromRawCommand "./tools/nuget.exe" ["restore"; solutionFile]
  |> Proc.run // start with the above configuration

Target.create "Restore" (fun _ ->
    DotNet.restore id "src/IO.Ably.sln"
    nugetRestore "src/IO.Ably.sln" |> ignore
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



type TestRun =
  | Method of string
  | UnitTests
  | IntegrationTests

let findNextTestPath (resultsPath:string) =
    [ 1 .. 100 ] 
    |> Seq.map ( fun i -> resultsPath.Replace(".xml", sprintf "-%d.xml" i))
    |> Seq.find (File.exists >> not)
    
let trimTestMethod (testMethod:string) = 
    match testMethod.Contains("(") with
    | true -> testMethod.Substring(0, testMethod.IndexOf("("))
    | false -> testMethod

let findFailedXUnitTests (resultsPath:string) =
    let doc = XDocument.Load(resultsPath)
    let nodes = doc.XPathSelectElements("//test-case[@success='False']")

    nodes 
    |> Seq.map (fun node -> (node.Attribute(XName.Get("name"))).Value)
    |> Seq.map trimTestMethod

let findFailedDotnetTestTests (resultsPath:string) =
    let doc = XDocument.Load(resultsPath)
    let nodes = doc.XPathSelectElements("//UnitTestResult[@outcome='Failed']")

    nodes 
    |> Seq.map (fun node -> (node.Attribute(XName.Get("testName"))).Value)
    |> Seq.map trimTestMethod  

let runStandardTests testToRun = 
  Directory.ensure testResultsDir
  Directory.ensure testResultsDir
  Trace.log " --- Testing net core version --- "
  let project = !! ("src/IO.Ably.Tests.DotNetCore20/*.csproj") |> Seq.head
 
  match testToRun with
  | Method testMethodName -> 
                          let logsPath = findNextTestPath(Path.combine testResultsDir "tests-netstandard.trx")
                          DotNet.test (fun opts -> { opts with Configuration = configuration
                                                               Filter = Some testMethodName
                                                               Logger = Some( "trx;logfilename=" + (Path.combine testResultsDir "integration-tests-standard.trx"))
                                         })
                                      project
                          logsPath                             
  | UnitTests -> 
                 let logsPath = Path.combine testResultsDir "tests-netstandard-unit.trx"
                 DotNet.test (fun opts -> { opts with Configuration = configuration
                                                      Filter = Some ("type!=integration")
                                                      Logger = Some( "trx;logfilename=" + logsPath)
                                         })
                              project
                 logsPath                              
  | IntegrationTests ->  
                         let logsPath = Path.combine testResultsDir "tests-netstandard-integration.trx"
                         try
                           DotNet.test (fun opts -> { opts with Configuration = configuration
                                                                Filter = Some ("type=integration")
                                                                Logger = Some( "trx;logfilename=" + logsPath)
                                           })
                                project
                          with 
                          | :? Fake.DotNet.MSBuildException -> 
                              printfn "Not all integration tests passed the first time"  

                         logsPath                                                                                


let runFrameworkTests testToRun = 
  Directory.ensure testResultsDir
  let testDir = Path.combine sourceDir "src/IO.Ably.Tests.NETFramework/bin/Release"
  let testDll = !! (Path.combine testDir "*.Tests.*.dll")
 
  match testToRun with
  | Method testMethodName -> 
                          let logsPath = findNextTestPath(Path.combine testResultsDir "xunit-netframework.xml")
                          testDll 
                          |> xUnit2 (fun p -> { p with NUnitXmlOutputPath = Some (  logsPath)
                                                       Method = Some (trimTestMethod testMethodName)
                               })
                          logsPath                             
  | UnitTests -> 
                 let logsPath = Path.combine testResultsDir "xunit-netframework-unit.xml"
                 testDll 
                 |> xUnit2 (fun p -> { p with NUnitXmlOutputPath = Some logsPath
                                              ExcludeTraits = [ ("type", "integration")]
                               })  
                 logsPath                             
  | IntegrationTests ->  
                         let logsPath = Path.combine testResultsDir "xunit-netframework-integration.xml"
                         testDll 
                         |> xUnit2 (fun p -> { p with NUnitXmlOutputPath = Some logsPath
                                                      IncludeTraits = [ ("type", "integration")]
                                                      TimeOut = TimeSpan.FromMinutes(20.)
                                                      Parallel = ParallelMode.Collections
                                                      ErrorLevel = TestRunnerErrorLevel.DontFailBuild // TODO: Make sure to retry the tests
                               }) 
                         logsPath                                                                                

Target.create "NetFramework.Integration.Rerun" (fun _ -> 
    
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


    let logsPath = Path.combine testResultsDir "xunit-netframework-integration.xml"
    
    let failedTestNames = findFailedXUnitTests logsPath

    for test in failedTestNames do
        runFrameworkTests (Method test) |> ignore  
)

Target.create "NetFramework - Unit Tests" (fun _ ->
    
    runFrameworkTests UnitTests |> ignore
)

Target.create "NetFramework - Integration Tests" ( fun _ -> 

    let logs = runFrameworkTests IntegrationTests

    let failedTestNames = findFailedXUnitTests logs

    for test in failedTestNames do
        runFrameworkTests (Method test) |> ignore   
)

Target.create "NetStandard - Unit Tests" (fun _ ->
    runStandardTests UnitTests |> ignore
)

Target.create "NetStandard - Integration Tests" (fun _ ->

    let logs = runFrameworkTests IntegrationTests

    let failedTestNames = findFailedDotnetTestTests logs

    for test in failedTestNames do
        runFrameworkTests (Method test) |> ignore 
)

Target.create "Prepare" ignore
Target.create "Build.NetFramework" ignore
Target.create "Build.NetStandard" ignore
Target.create "Test.NetFramework" ignore
Target.create "Test.NetStandard" ignore

"Clean"
  ==> "Restore"
  ==> "Prepare"

"Prepare" 
  ==> "NetFramework - Build"
  ==> "Build.NetFramework"

"Prepare"
  ==> "NetStandard - Build"
  ==> "Build.NetStandard"

"Build.NetFramework" 
  ==> "NetFramework - Unit Tests"

"NetFramework - Unit Tests" 
  ==> "NetFramework - Integration Tests"
  ==> "Test.NetFramework"

"Build.NetStandard"
  ==> "NetStandard - Unit Tests"

"NetStandard - Unit Tests"
  ==> "NetStandard - Integration Tests"
  ==> "Test.NetStandard"


Target.runOrDefaultWithArguments  "Test.NetFramework"