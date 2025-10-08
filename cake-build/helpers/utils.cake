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
