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
        
        return settings
            .SetConfiguration(config)
            .SetVerbosity(Verbosity.Quiet)
            .WithProperty("Optimize", "True")
            .WithProperty("DebugSymbols", "True")
            .WithProperty("GenerateDocumentationFile", "true")
            .WithProperty("Deterministic", "true")
            .WithProperty("ContinuousIntegrationBuild", isCI ? "true" : "false")
            .WithProperty("dummy", "property"); // Workaround for MSBuild issue (same as FAKE)
    }
}

var buildConfig = new BuildConfiguration(Context);
