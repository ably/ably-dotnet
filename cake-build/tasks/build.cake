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

// Helper method to restore a solution, try both nuget restore and dotnet restore
void RestoreSolution(FilePath solutionPath)
{
    Information($"Restoring NuGet packages for {solutionPath.GetFilename()}...");

    // This is needed to restore deprecated Xamarin projects on Windows and macOS/Linux.
    // Needed for projects using old packages.config format for maintaining dependencies.
    // This will not be needed once deprecated projects are removed.
    Information("Running NuGet restore...");
    try
    {
        if (IsRunningOnWindows())
        {
            Information("Windows system detected, running direct NuGetRestore command");
            NuGetRestore(solutionPath.FullPath);
        }
        else
        {
            Information("macOS/Linux system detected, running nuget restore from CLI");
            // On macOS/Linux, use nuget command (installed via mono)
            StartProcess("nuget", new ProcessSettings
            {
                Arguments = $"restore {solutionPath.FullPath}"
            });
        }
    }
    catch (Exception ex)
    {
        Warning($"NuGet restore failed: {ex.Message}");
    }

    // dotnet restore (all platforms, for SDK-style projects)
    try
    {
        Information("Running dotnet restore...");
        // Suppress NU1903 vulnerability warning for Newtonsoft.Json 9.0.1 (known issue, accepted risk)
        // Also supress restore warning as errors NU1503 for xamarin/old style projects
        var restoreSettings = new DotNetRestoreSettings
        {
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("WarningsNotAsErrors", "NU1903;NU1503")
                .WithProperty("NoWarn", "NU1903;NU1503")
        };
        DotNetRestore(solutionPath.FullPath, restoreSettings);
        Information($"✓ dotnet restore completed");
    }
    catch (Exception e)
    {
        Warning($"dotnet restore failed: {e.Message}");
    }
}

Task("Restore")
    .Does(() =>
{
    // FAKE restores the main IO.Ably.sln solution, not IO.Ably.NetStandard.sln
    RestoreSolution(paths.MainSolution);
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
    
    settings = settings.WithTarget("Build");
    
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
    
    // Suppress NU1903 vulnerability warning for Newtonsoft.Json 9.0.1 (known issue, accepted risk)
    var msbuildSettings = new DotNetMSBuildSettings()
        .WithProperty("WarningsNotAsErrors", "NU1903")
        .WithProperty("NoWarn", "NU1903");
    
    if (!string.IsNullOrEmpty(defineConstants))
    {
        msbuildSettings = msbuildSettings.WithProperty("DefineConstants", defineConstants);
    }
    
    settings.MSBuildSettings = msbuildSettings;
    
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
        var nugetSettings = new NuGetRestoreSettings();
        
        // Use local nuget.exe if available
        var nugetPath = paths.Root.CombineWithFilePath("tools/nuget.exe");
        if (FileExists(nugetPath))
        {
            nugetSettings.ToolPath = nugetPath;
        }
        
        NuGetRestore(paths.XamarinSolution.FullPath, nugetSettings);
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
    
    settings = settings.WithTarget("Build");
    
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
