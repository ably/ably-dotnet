///////////////////////////////////////////////////////////////////////////////
// PACKAGE TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Package-Build-All")
    .IsDependentOn("Prepare")
    .IsDependentOn("Version")
    .Does(() =>
{
    Information("Building all projects for packaging...");
    
    if (!FileExists(paths.PackageSolution))
    {
        Warning($"Package solution not found: {paths.PackageSolution}");
        return;
    }
    
    var settings = buildConfig.ApplyStandardSettings(
        new MSBuildSettings(), 
        "Release"
    );
    
    settings = buildConfig.ApplyPackageSettings(settings);
    settings.WithTarget("Build");
    
    MSBuild(paths.PackageSolution, settings);
});

Task("Package-Merge-JsonNet")
    .IsDependentOn("Package-Build-All")
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
        var packagedPath = binPath.Combine("packaged");
        
        if (!DirectoryExists(binPath))
        {
            Warning($"Bin directory not found for {project}, skipping...");
            continue;
        }
        
        Information($"Processing {project}...");
        
        // Copy all IO.Ably* files to packaged folder
        var ablyFiles = GetFiles(binPath.FullPath + "/IO.Ably*");
        EnsureDirectoryExists(packagedPath);
        CopyFiles(ablyFiles, packagedPath);
        
        // Merge Newtonsoft.Json
        ilRepackHelper.MergeJsonNet(binPath, packagedPath);
    }
});

Task("Package-Create-NuGet")
    .IsDependentOn("Package-Merge-JsonNet")
    .WithCriteria(() => !string.IsNullOrEmpty(version))
    .Does(() =>
{
    Information($"Creating NuGet package version {version}...");
    
    var nuspecFile = "./nuget/io.ably.nuspec";
    
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
    var nugetPath = "./tools/nuget.exe";
    if (FileExists(nugetPath))
    {
        nugetSettings.ToolPath = nugetPath;
    }
    
    NuGetPack(nuspecFile, nugetSettings);
    
    Information($"✓ Package created: ably.io.{version}.nupkg");
});

Task("Package-Unity")
    .IsDependentOn("Build.NetStandard")
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
            Arguments = "clone https://github.com/ably-forks/unity-packager.git -b v1.0.0 unity-packager",
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
        Arguments = $"run --project {unityPackagerProject.FullPath} " +
                   $"-project unity -output {outputPath.FullPath} -dir Assets/Ably",
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

Task("PushPackage-Build-All")
    .IsDependentOn("Prepare")
    .IsDependentOn("Version")
    .Does(() =>
{
    Information("Building push notification packages...");
    
    if (!FileExists(paths.PushPackageSolution))
    {
        Warning($"Push package solution not found: {paths.PushPackageSolution}");
        return;
    }
    
    var settings = buildConfig.ApplyStandardSettings(
        new MSBuildSettings(), 
        "Package"
    );
    
    settings = buildConfig.ApplyPackageSettings(settings);
    settings.WithTarget("Build");
    
    MSBuild(paths.PushPackageSolution, settings);
});

Task("PushPackage-Create-NuGet")
    .IsDependentOn("PushPackage-Build-All")
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
    var nugetPath = "./tools/nuget.exe";
    if (FileExists(nugetPath))
    {
        nugetSettings.ToolPath = nugetPath;
    }
    
    // Android package
    var androidNuspec = "./nuget/io.ably.push.android.nuspec";
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
    var iosNuspec = "./nuget/io.ably.push.ios.nuspec";
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

///////////////////////////////////////////////////////////////////////////////
// PUBLIC TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
    .IsDependentOn("Package-Create-NuGet");

Task("Package.WithUnity")
    .IsDependentOn("Package-Create-NuGet")
    .IsDependentOn("Package-Unity");

Task("PushPackage")
    .IsDependentOn("PushPackage-Create-NuGet");
