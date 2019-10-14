#r "paket:
nuget Fake.Core.Target
//"

#load "./build-steps.fsx"

open Fake.Core
open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "NetFramework - Build"
  ==> "NetFramework - Unit Tests"

// *** Start Build ***
Target.runOrDefault "NetFramework - Unit Tests"