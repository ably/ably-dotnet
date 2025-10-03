///////////////////////////////////////////////////////////////////////////////
// BUILD CONFIGURATION HELPERS
///////////////////////////////////////////////////////////////////////////////

public class BuildConfiguration
{
    private readonly ICakeContext _context;
    
    public BuildConfiguration(ICakeContext context)
    {
        _context = context;
    }
    
    public string GetConfiguration(string baseConfig, bool isPackage = false)
    {
        if (isPackage)
        {
            // Package builds use special configuration
            return baseConfig == "Release" ? "Package" : baseConfig;
        }
        return baseConfig;
    }
    
    public MSBuildSettings ApplyPackageSettings(MSBuildSettings settings)
    {
        return settings
            .WithProperty("StyleCopEnabled", "True")
            .WithProperty("Package", "True")
            .WithProperty("DefineConstants", "PACKAGE")
            .WithProperty("GenerateDocumentationFile", "true");
    }
    
    public MSBuildSettings ApplyStandardSettings(MSBuildSettings settings, string config)
    {
        var isCI = _context.BuildSystem().IsLocalBuild == false;
        
        var result = settings
            .SetConfiguration(config)
            .SetVerbosity(Verbosity.Quiet)
            .WithProperty("Optimize", "True")
            .WithProperty("DebugSymbols", "True")
            .WithProperty("GenerateDocumentationFile", "true");
        
        // Deterministic builds: Ensures byte-for-byte identical binaries from same source
        // Benefits: Reproducible builds, better caching, security verification
        // Requires: Full Git history and SourceLink packages
        result = result.WithProperty("Deterministic", "true");
        
        // ContinuousIntegrationBuild: Enables source link and embeds Git commit info
        // Benefits: Better debugging (step into library code), traceability
        // Required for: Proper source link functionality in NuGet packages
        result = result.WithProperty("ContinuousIntegrationBuild", isCI ? "true" : "false");
        
        // Workaround for MSBuild issue (same as FAKE)
        result = result.WithProperty("dummy", "property");
        
        return result;
    }
}

var buildConfig = new BuildConfiguration(Context);
