///////////////////////////////////////////////////////////////////////////////
// FRAMEWORK DETECTION HELPERS
///////////////////////////////////////////////////////////////////////////////

public class FrameworkDetector
{
    private readonly ICakeContext _context;
    
    public FrameworkDetector(ICakeContext context)
    {
        _context = context;
    }
    
    public string[] GetTargetFrameworks()
    {
        var frameworks = new List<string> { "netstandard2.0" };
        
        try
        {
            // Check installed SDKs
            IEnumerable<string> output = new List<string>();
            _context.StartProcess("dotnet", new ProcessSettings
            {
                Arguments = "--list-sdks",
                RedirectStandardOutput = true
            }, out output);
            
            foreach (var line in output)
            {
                if (line.Contains("6.0") && !frameworks.Contains("net6.0"))
                    frameworks.Add("net6.0");
                if (line.Contains("7.0") && !frameworks.Contains("net7.0"))
                    frameworks.Add("net7.0");
                if (line.Contains("8.0") && !frameworks.Contains("net8.0"))
                    frameworks.Add("net8.0");
            }
        }
        catch (Exception ex)
        {
            _context.Warning($"Could not detect installed SDKs: {ex.Message}");
        }
        
        return frameworks.ToArray();
    }
    
    public bool IsFrameworkAvailable(string framework)
    {
        var available = GetTargetFrameworks();
        return available.Contains(framework);
    }
}

var frameworkDetector = new FrameworkDetector(Context);
