///////////////////////////////////////////////////////////////////////////////
// UTILITY HELPERS
///////////////////////////////////////////////////////////////////////////////

/// <summary>
/// Restores NuGet packages for a solution using both NuGet restore and dotnet restore.
/// This handles both legacy packages.config projects and modern SDK-style projects.
/// </summary>
/// <param name="solutionPath">Path to the solution file to restore</param>
public void RestoreSolution(FilePath solutionPath)
{
    if (!FileExists(solutionPath))
    {
        throw new Exception($"Solution file not found: {solutionPath.FullPath}");
    }

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
            NuGetRestore(solutionPath.FullPath, new NuGetRestoreSettings
            {
                Verbosity = NuGetVerbosity.Quiet
            });
        }
        else
        {
            Information("macOS/Linux system detected, running nuget restore from CLI");
            // On macOS/Linux, use nuget command (installed via mono)
            StartProcess("nuget", new ProcessSettings
            {
                Arguments = $"restore \"{solutionPath.FullPath}\" -Verbosity quiet"
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
        // Suppress restore warning as errors NU1503 for xamarin/old style projects
        var restoreSettings = new DotNetRestoreSettings
        {
            MSBuildSettings = new DotNetMSBuildSettings()
                .WithProperty("WarningsNotAsErrors", "NU1503")
                .WithProperty("NoWarn", "NU1503")
        };
        DotNetRestore(solutionPath.FullPath, restoreSettings);
        Information($"✓ dotnet restore completed");
    }
    catch (Exception e)
    {
        Warning($"dotnet restore failed: {e.Message}");
    }
}

/// <summary>
/// Cleans build outputs for all projects by deleting bin and obj directories.
/// Searches for all .csproj files in the src directory and cleans each one.
/// </summary>
public void CleanSolution()
{
    Information("Cleaning all build outputs...");
    
    // Get all project files in the main src directory
    var projectFiles = GetFiles($"{paths.Src}/**/*.csproj")
        .Where(f => !f.FullPath.Contains("node_modules") &&
                    !f.FullPath.Contains(".git") &&
                    !f.FullPath.Contains("packages"))
        .ToList();
    
    Information($"Found {projectFiles.Count} project(s) to clean");
    
    foreach (var projectFile in projectFiles)
    {
        Information($"Cleaning project: {projectFile.GetFilename()}");
        
        var projectDir = projectFile.GetDirectory();
        
        // Clean bin directory
        var binDir = projectDir.Combine("bin");
        if (DirectoryExists(binDir))
        {
            try
            {
                DeleteDirectory(binDir, new DeleteDirectorySettings {
                    Recursive = true,
                    Force = true
                });
                Information($"  ✓ Cleaned {binDir}");
            }
            catch (Exception ex)
            {
                Warning($"  Failed to clean {binDir}: {ex.Message}");
            }
        }
        
        // Clean obj directory
        var objDir = projectDir.Combine("obj");
        if (DirectoryExists(objDir))
        {
            try
            {
                DeleteDirectory(objDir, new DeleteDirectorySettings {
                    Recursive = true,
                    Force = true
                });
                Information($"  ✓ Cleaned {objDir}");
            }
            catch (Exception ex)
            {
                Warning($"  Failed to clean {objDir}: {ex.Message}");
            }
        }
    }
    
    Information($"✓ Completed cleaning all projects");
}
