///////////////////////////////////////////////////////////////////////////////
// PACKAGE TASKS (Internal)
///////////////////////////////////////////////////////////////////////////////

Task("_Restore_Package")
    .Does(() =>
{
    RestoreSolution(paths.PackageSolution);
});

Task("_Package_Build_All")
    .IsDependentOn("_Clean")
    .IsDependentOn("_Version")
    .IsDependentOn("_Restore_Package")
    .Does(() =>
{
    Information("Building all projects for packaging...");
    
    var settings = buildConfig.ApplyStandardSettings(
        new MSBuildSettings(),
        "Release"
    );
    
    settings = buildConfig.ApplyPackageSettings(settings);
    settings = settings.WithTarget("Build");
    
    MSBuild(paths.PackageSolution, settings);
});

Task("_Package_Merge_JsonNet")
    .IsDependentOn("_Package_Build_All")
    .Does(() =>
{
    Information("Merging Newtonsoft.Json into Ably assemblies...");
    
    var projectsToMerge = new[] 
    { 
        "IO.Ably.Android", 
        "IO.Ably.iOS", 
        "IO.Ably.NETFramework" 
    };
    
    foreach (var project in projectsToMerge)
    {
        var projectPath = paths.Src.Combine(project);
        
        if (!DirectoryExists(projectPath))
        {
            Warning($"Project directory not found: {project}, skipping...");
            continue;
        }
        
        var binPath = projectPath.Combine("bin/Release");
        var packagedPath = binPath.Combine("Packaged");
        
        if (!DirectoryExists(binPath))
        {
            Warning($"Bin directory not found for {project}, skipping...");
            continue;
        }
        
        Information($"Processing {project}...");
        
        // Copy all IO.Ably* files to Packaged folder
        var ablyFiles = GetFiles(binPath.Combine("IO.Ably*").FullPath);
        EnsureDirectoryExists(packagedPath);
        CopyFiles(ablyFiles, packagedPath);
        
        // Merge Newtonsoft.Json
        ilRepackHelper.MergeJsonNet(binPath, packagedPath);
    }
});

Task("_Package_Merge_DeltaCodec")
    .IsDependentOn("_Package_Merge_JsonNet")
    .Does(() =>
{
    Information("Merging DeltaCodec into Ably assemblies for all platforms...");
    
    // Legacy platforms (already in packaged folder after JsonNet merge)
    var legacyProjects = new[]
    {
        "IO.Ably.Android",
        "IO.Ably.iOS",
        "IO.Ably.NETFramework"
    };
    
    foreach (var project in legacyProjects)
    {
        var projectPath = paths.Src.Combine(project);
        
        if (!DirectoryExists(projectPath))
        {
            Warning($"Project directory not found: {project}, skipping...");
            continue;
        }
        
        var binPath = projectPath.Combine("bin/Release");
        var packagedPath = binPath.Combine("Packaged");
        
        if (!DirectoryExists(packagedPath))
        {
            Warning($"Packaged directory not found for {project}, skipping...");
            continue;
        }
        
        Information($"Merging DeltaCodec for {project}...");
        ilRepackHelper.MergeDeltaCodec(binPath, packagedPath);
    }
});

Task("_Package_Create_NuGet")
    .IsDependentOn("_Package_Merge_DeltaCodec")
    .WithCriteria(() => !string.IsNullOrEmpty(version))
    .Does(() =>
{
    Information($"Creating NuGet package version {version}...");
    
    var nuspecFile = paths.Root.CombineWithFilePath("nuget/io.ably.nuspec");
    
    if (!FileExists(nuspecFile))
    {
        throw new Exception($"Nuspec file not found: {nuspecFile}");
    }
    
    var nugetSettings = new NuGetPackSettings
    {
        Version = version,
        Properties = new Dictionary<string, string>
        {
            { "Configuration", "Release" }
        },
        OutputDirectory = paths.Root
    };
    
    // Use local nuget.exe if available
    var nugetPath = paths.Root.CombineWithFilePath("tools/nuget.exe");
    if (FileExists(nugetPath))
    {
        nugetSettings.ToolPath = nugetPath;
    }
    
    NuGetPack(nuspecFile, nugetSettings);
    
    Information($"✓ Package created: ably.io.{version}.nupkg");
});

Task("_Restore_Push_Package")
    .Does(() =>
{
    RestoreSolution(paths.PushPackageSolution);
});

Task("_PushPackage_Build_All")
    .IsDependentOn("_Clean")
    .IsDependentOn("_Version")
    .IsDependentOn("_Restore_Push_Package")
    .Does(() =>
{
    Information("Building push notification packages...");
    
    var settings = buildConfig.ApplyStandardSettings(
        new MSBuildSettings(),
        "Package"
    );
    
    settings = buildConfig.ApplyPackageSettings(settings);
    settings = settings.WithTarget("Build");
    
    MSBuild(paths.PushPackageSolution, settings);
});

Task("_PushPackage_Create_NuGet")
    .IsDependentOn("_PushPackage_Build_All")
    .WithCriteria(() => !string.IsNullOrEmpty(version))
    .Does(() =>
{
    Information($"Creating push notification packages version {version}...");
    
    var nugetSettings = new NuGetPackSettings
    {
        Version = version,
        Properties = new Dictionary<string, string>
        {
            { "Configuration", "Release" }
        },
        OutputDirectory = paths.Root
    };
    
    // Use local nuget.exe if available
    var nugetPath = paths.Root.CombineWithFilePath("tools/nuget.exe");
    if (FileExists(nugetPath))
    {
        nugetSettings.ToolPath = nugetPath;
    }
    
    // Android package
    var androidNuspec = paths.Root.CombineWithFilePath("nuget/io.ably.push.android.nuspec");
    if (FileExists(androidNuspec))
    {
        NuGetPack(androidNuspec, nugetSettings);
        Information($"✓ Package created: ably.io.push.android.{version}.nupkg");
    }
    else
    {
        Warning($"Android nuspec not found: {androidNuspec}");
    }
    
    // iOS package
    var iosNuspec = paths.Root.CombineWithFilePath("nuget/io.ably.push.ios.nuspec");
    if (FileExists(iosNuspec))
    {
        NuGetPack(iosNuspec, nugetSettings);
        Information($"✓ Package created: ably.io.push.ios.{version}.nupkg");
    }
    else
    {
        Warning($"iOS nuspec not found: {iosNuspec}");
    }
});

Task("_Package_Unity")
    .WithCriteria(() => !string.IsNullOrEmpty(version))
    .Does(() =>
{
    Information($"Creating Unity package version {version}...");
    
    var unityPackagerPath = paths.Root.Combine("unity-packager");
    var outputPath = paths.Root.CombineWithFilePath($"ably.io.{version}.unitypackage");
    
    // Clone unity-packager if not exists
    if (!DirectoryExists(unityPackagerPath))
    {
        Information("Cloning unity-packager repository...");
        StartProcess("git", new ProcessSettings
        {
            Arguments = $"clone https://github.com/ably-forks/unity-packager.git -b v1.0.0 \"{unityPackagerPath.FullPath}\"",
            WorkingDirectory = paths.Root
        });
    }
    
    var unityPackagerProject = unityPackagerPath.CombineWithFilePath("UnityPackageExporter/UnityPackageExporter.csproj");
    
    if (!FileExists(unityPackagerProject))
    {
        Warning("Unity packager project not found, skipping Unity package creation");
        return;
    }
    
    Information("Building Unity package...");
    StartProcess("dotnet", new ProcessSettings
    {
        Arguments = $"run --project \"{unityPackagerProject.FullPath}\" " +
                   $"-project \"{paths.Root.Combine("unity").FullPath}\" -output \"{outputPath.FullPath}\" -dir Assets/Ably",
        WorkingDirectory = paths.Root
    });
    
    if (FileExists(outputPath))
    {
        Information($"✓ Unity package created: {outputPath}");
    }
    else
    {
        Warning("Unity package was not created");
    }
});

///////////////////////////////////////////////////////////////////////////////
// PUBLIC TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
    .Description("Create main NuGet package (ably.io)")
    .IsDependentOn("_Package_Create_NuGet");

Task("PushPackage")
    .Description("Create push notification packages (Android & iOS)")
    .IsDependentOn("_PushPackage_Create_NuGet");

Task("UnityPackage")
    .Description("Create Unity package")
    .IsDependentOn("_Package_Unity");
