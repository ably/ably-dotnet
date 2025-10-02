#addin nuget:?package=Cake.Git&version=4.0.0
#addin nuget:?package=Cake.Compression&version=0.3.0

#load "helpers/paths.cake"
#load "helpers/tools.cake"
#load "helpers/test-retry.cake"
#load "helpers/build-config.cake"
#load "helpers/frameworks.cake"
#load "tasks/build.cake"
#load "tasks/test.cake"
#load "tasks/package.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Build.NetStandard");
var configuration = Argument("configuration", "Release");
var version = Argument("version", "");
var defineConstants = Argument("define", "");
var framework = Argument("framework", "");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Information("========================================");
    Information("Cake Build System for ably-dotnet");
    Information("========================================");
    Information($"Target: {target}");
    Information($"Configuration: {configuration}");
    if (!string.IsNullOrEmpty(version))
        Information($"Version: {version}");
    if (!string.IsNullOrEmpty(framework))
        Information($"Framework: {framework}");
    Information($"Platform: {(IsRunningOnWindows() ? "Windows" : IsRunningOnUnix() ? "Unix" : "macOS")}");
});

Teardown(ctx =>
{
    Information("========================================");
    Information("Build completed");
    Information("========================================");
});

///////////////////////////////////////////////////////////////////////////////
// TASK EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
