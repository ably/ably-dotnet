///////////////////////////////////////////////////////////////////////////////
// BUILD TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    Information("Cleaning build directories...");
    CleanDirectory(paths.BuildOutput);
    CleanDirectory(paths.Package);
    EnsureDirectoryExists(paths.TestResults);
    EnsureDirectoryExists(paths.Package);
});

Task("Restore")
    .Does(() =>
{
    Information("Restoring NuGet packages...");
    
    // FAKE restores the main IO.Ably.sln solution, not IO.Ably.NetStandard.sln
    var mainSolution = paths.Src.CombineWithFilePath("IO.Ably.sln");
    
    // Use NuGet CLI for .NET Framework projects (packages.config)
    // This is critical for both Windows and macOS/Linux with Mono
    Information("Running NuGet restore...");
    try
    {
        if (IsRunningOnWindows())
        {
            NuGetRestore(mainSolution.FullPath);
        }
        else
        {
            // On macOS/Linux, use nuget command (installed via mono)
            StartProcess("nuget", new ProcessSettings
            {
                Arguments = $"restore {mainSolution.FullPath}"
            });
        }
    }
    catch (Exception ex)
    {
        Warning($"NuGet restore failed: {ex.Message}");
        // Continue anyway as dotnet restore might handle it
    }
    
    Information("Running dotnet restore...");
    DotNetRestore(mainSolution.FullPath);
});

Task("Version")
    .WithCriteria(() => !string.IsNullOrEmpty(version))
    .Does(() =>
{
    Information($"Setting version to {version}");
    
    var assemblyInfoPath = paths.Src.CombineWithFilePath("CommonAssemblyInfo.cs");
    
    CreateAssemblyInfo(assemblyInfoPath, new AssemblyInfoSettings
    {
        Company = "Ably",
        Product = "Ably .NET Library",
        Copyright = $"Copyright © Ably {DateTime.Now.Year}",
        Version = version,
        FileVersion = version,
        InformationalVersion = version
    });
});

Task("NetFramework-Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Building .NET Framework solution...");
    
    var settings = buildConfig.ApplyStandardSettings(
        new MSBuildSettings(), 
        configuration
    );
    
    settings.WithTarget("Build");
    
    MSBuild(paths.NetFrameworkSolution, settings);
});

Task("NetStandard-Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    Information("Building .NET Standard solution...");
    
    var settings = new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    };
    
    if (!string.IsNullOrEmpty(defineConstants))
    {
        settings.MSBuildSettings = new DotNetMSBuildSettings()
            .WithProperty("DefineConstants", defineConstants);
    }
    
    DotNetBuild(paths.NetStandardSolution.FullPath, settings);
});

Task("Restore-Xamarin")
    .Does(() =>
{
    Information("Restoring Xamarin packages...");
    
    if (!FileExists(paths.XamarinSolution))
    {
        Warning("Xamarin solution not found, skipping restore");
        return;
    }
    
    // Use NuGet CLI for Xamarin restore (matches FAKE behavior)
    try
    {
        Information($"Running NuGet restore for {paths.XamarinSolution.FullPath}...");
        NuGetRestore(paths.XamarinSolution.FullPath);
    }
    catch (Exception ex)
    {
        Warning($"NuGet restore for Xamarin failed: {ex.Message}");
        throw;
    }
});

Task("Xamarin-Build")
    .IsDependentOn("Restore-Xamarin")
    .Does(() =>
{
    Information("Building Xamarin solution...");
    
    if (!FileExists(paths.XamarinSolution))
    {
        Warning("Xamarin solution not found, skipping build");
        return;
    }
    
    var settings = buildConfig.ApplyStandardSettings(
        new MSBuildSettings(), 
        configuration
    );
    
    settings.WithTarget("Build");
    
    MSBuild(paths.XamarinSolution, settings);
});

///////////////////////////////////////////////////////////////////////////////
// PUBLIC TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Prepare")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore");

Task("Build.NetFramework")
    .IsDependentOn("Prepare")
    .IsDependentOn("NetFramework-Build");

Task("Build.NetStandard")
    .IsDependentOn("Prepare")
    .IsDependentOn("NetStandard-Build");

Task("Build.Xamarin")
    .IsDependentOn("Prepare")
    .IsDependentOn("Xamarin-Build");
