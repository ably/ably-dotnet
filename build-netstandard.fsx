#r "paket:
nuget Fake.Core.Target 
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Testing.XUnit2
nuget Fake.DotNet.ILMerge
//"

#load "./build-steps.fsx"

open Fake.Core
open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "NetStandard - Build"
  ==> "NetStandard - Unit Tests"

// *** Start Build ***
Target.runOrDefault "NetStandard - Unit Tests"