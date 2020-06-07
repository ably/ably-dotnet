#r "paket:
nuget Fake.Core.Target 
nuget Fake.Core.CommandLineParsing
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.XUnit2
nuget Fake.DotNet.NuGet
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
open Fake.DotNet.NuGet
open System.Text.RegularExpressions

let currentDir = __SOURCE_DIRECTORY__
let netstandardTestDir = "src/IO.Ably.Tests.DotNetCore20"
let xUnit2 = XUnit2.run

let NetStandardSolution = "src/IO.Ably.NetStandard.sln"
let NetFrameworkSolution = "src/IO.Ably.NetFramework.sln"
let buildDir = Path.combine currentDir "build"
let srcDir = Path.combine currentDir "src"
let testResultsDir = Path.combine buildDir "tests"
let packageDir = Path.combine buildDir "package"
let configuration = DotNet.Release
let packageSolution = "src/IO.Ably.Package.sln"
let buildMode = Environment.environVarOrDefault "buildMode" "Release"

let cli = """
usage: prog [options]

options:
 -t <str>       Target
 -v <str>       Version
"""

// retrieve the fake 5 context information
let ctx = Context.forceFakeContext ()
// get the arguments
let args = ctx.Arguments
let parser = Docopt(cli)
let parsedArguments = parser.Parse(args |> List.toArray)

let version = match DocoptResult.tryGetArgument "-v" parsedArguments  with
                | None -> ""
                | Some version -> version

let mergeJsonNet path outputPath = 
  let target = Path.combine path "IO.Ably.dll"
  let docsFile = Path.combine path "IO.Ably.xml"
  let out = Path.combine outputPath "IO.Ably.dll"
  
  Directory.ensure outputPath

  CreateProcess.fromRawCommand "./tools/ilrepack.exe" 
      [
        "/lib:" + path
        "/targetplatform:v4"
        "/internalize"
        "/attr:" + target
        "/keyfile:IO.Ably.snk"
        "/parallel"
        "/out:" + out
        target
        Path.combine path "Newtonsoft.Json.dll"
        ]
  |> Proc.run // start with the above configuration
  |> ignore

  // Copy the xml docs
  if File.exists docsFile then Shell.copy outputPath [ docsFile ]


// *** Define Targets ***
Target.create "Clean" (fun _ ->
  Trace.log (sprintf "Current dir: %s" currentDir)
  Trace.log " --- Removing build folder ---"
  Directory.delete(buildDir) 
  Directory.delete(packageDir) 

  Directory.ensure testResultsDir
  Directory.ensure packageDir
)

Target.create "Version" (fun _ -> 
  AssemblyInfoFile.createCSharp "./src/CommonAssemblyInfo.cs"
      [   
          AssemblyInfo.Company "Ably Realtime"
          AssemblyInfo.Description "Client for ably.io realtime service"
          AssemblyInfo.Product "Ably .Net Library"
          AssemblyInfo.Version version
          AssemblyInfo.FileVersion version
      ]
)


let nugetRestore solutionFile = 
  CreateProcess.fromRawCommand "./tools/nuget.exe" ["restore"; solutionFile]
  |> Proc.run // start with the above configuration

Target.create "Restore" (fun _ ->
    //DotNet.restore id "src/IO.Ably.sln"
    nugetRestore "src/IO.Ably.sln" |> ignore
    CreateProcess.fromRawCommand "dotnet" ["restore"; "src/IO.Ably.sln"] 
    |> Proc.run |> ignore
)

Target.create "NetFramework - Build" (fun _ ->
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
)

type TestRun =
  | Method of string
  | UnitTests
  | IntegrationTests

let findNextTrxTestPath (resultsPath:string) =
    [ 1 .. 100 ] 
    |> Seq.map ( fun i -> resultsPath.Replace(".trx", sprintf "-%d.trx" i))
    |> Seq.find (File.exists >> not)

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
    let xml = File.readAsString resultsPath
    let tidyXml = Regex.Replace(xml, @"xmlns=\""[^\""]+\""", "") // Remove the namespace to make xpath queries easier
    let doc = XDocument.Parse(tidyXml);
    let nodes = doc.XPathSelectElements("//UnitTestResult[@outcome='Failed']")
    printfn "Nodes found: %d" (nodes |> Seq.length)

    nodes 
    |> Seq.map (fun node -> (node.Attribute(XName.Get("testName"))).Value)
    |> Seq.map trimTestMethod  
    |> Seq.toList

let runStandardTests testToRun = 
  Directory.ensure testResultsDir
  Trace.log " --- Testing net core version --- "
  let project = Path.combine currentDir "src/IO.Ably.Tests.DotNetCore20/IO.Ably.Tests.DotNetCore20.csproj"
  
  match testToRun with
  | Method testMethodName -> 
                          let logsPath = findNextTrxTestPath(Path.combine testResultsDir "tests-netstandard.trx")
                          DotNet.test (fun opts -> { opts with Configuration = configuration
                                                               Filter = Some testMethodName
                                                               Logger = Some( "trx;logfilename=" + logsPath)
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
  let testDir = Path.combine currentDir "src/IO.Ably.Tests.NETFramework/bin/Release"
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

Target.create "NetStandard - Build" (fun _ ->
  DotNet.build (fun opts -> {
    opts with Configuration = configuration
  }) NetStandardSolution
)

Target.create "NetStandard - Unit Tests" (fun _ ->
    runStandardTests UnitTests |> ignore
)

Target.create "NetStandard - Integration Tests" (fun _ ->

    let logs = runStandardTests IntegrationTests

    let failedTestNames = findFailedDotnetTestTests logs

    for test in failedTestNames do
        runStandardTests (Method test) |> ignore 
)

Target.create "Package - Build All" (fun _ -> 
  let setParams (defaults:MSBuildParams) =
        { defaults with
            Verbosity = Some(Quiet)
            Targets = ["Build"]
            Properties =
                [
                    "Optimize", "True"
                    "DebugSymbols", "True"
                    "Configuration", buildMode
                    "StyleCopEnabled", "True"
                    "Package", "True"
                    "DefineConstants", "PACKAGE"
                ]
         }
  MSBuild.build setParams packageSolution
)

Target.create "Package - Merge json.net" (fun _ -> 
  let projectsToMerge = [ "IO.Ably.Android"; "IO.Ably.iOS"; "IO.Ably.NETFramework" ]
  let paths = projectsToMerge 
              |> Seq.map (Path.combine "src")
              |> Seq.map (fun path -> sprintf "%s/bin/%s" path buildMode)

  paths 
  |> Seq.iter ( fun path -> mergeJsonNet path (Path.combine path "packaged"))

)

Target.create "Package - Create nuget" (fun _ -> 
  CreateProcess.fromRawCommand "./tools/nuget.exe" 
      [
        "pack"
        "./nuget/io.ably.nuspec"
        "-properties"
        sprintf "version=%s;configuration=Release" version 
        ]
  |> Proc.run // start with the above configuration
  |> ignore      
)

Target.create "Prepare" ignore
Target.create "Build.NetFramework" ignore
Target.create "Build.NetStandard" ignore
Target.create "Test.NetFramework" ignore
Target.create "Test.NetStandard" ignore

Target.create "Package" ignore

"Clean"
  ==> "Restore"
  ==> "Prepare"

"Prepare" 
  ==> "NetFramework - Build"
  ==> "Build.NetFramework"

"Prepare"
  ==> "NetStandard - Build"
  ==> "Build.NetStandard"

"Prepare"
  ==> "Version"
  ==> "Package - Build All"
  ==> "Package - Merge json.net"
  ==> "Package - Create nuget"
  ==> "Package"

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