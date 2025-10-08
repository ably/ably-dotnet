///////////////////////////////////////////////////////////////////////////////
// BUILD TASKS (Internal)
///////////////////////////////////////////////////////////////////////////////

Task("_Clean")
    .Does(() =>
{
    Information("Cleaning build directories...");
    CleanDirectory(paths.BuildOutput);
    CleanDirectory(paths.Package);
    EnsureDirectoryExists(paths.TestResults);
    EnsureDirectoryExists(paths.Package);
});

Task("_Restore_Main")
    .Does(() =>
{
    RestoreSolution(paths.MainSolution);
});

Task("_Version")
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

Task("_NetFramework_Build")
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

Task("_NetStandard_Build")
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

Task("_Restore_Xamarin")
    .Does(() =>
{
    RestoreSolution(paths.XamarinSolution);
});

Task("_Xamarin_Build")
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
// These are the tasks that should be called directly by users or CI/CD

// Public task: Build .NET Framework projects
Task("Build.NetFramework")
    .Description("Build .NET Framework solution")
    .IsDependentOn("_Clean")
    .IsDependentOn("_Restore_Main")
    .IsDependentOn("_NetFramework_Build");

// Public task: Build .NET Standard projects
Task("Build.NetStandard")
    .Description("Build .NET Standard solution")
    .IsDependentOn("_Clean")
    .IsDependentOn("_Restore_Main")
    .IsDependentOn("_NetStandard_Build");

// Public task: Build Xamarin projects
Task("Build.Xamarin")
    .Description("Build Xamarin solution (iOS & Android)")
    .IsDependentOn("_Clean")
    .IsDependentOn("_Restore_Xamarin")
    .IsDependentOn("_Xamarin_Build");
