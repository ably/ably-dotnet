using System.Net.Http;

///////////////////////////////////////////////////////////////////////////////
// LICENSE HELPERS
///////////////////////////////////////////////////////////////////////////////

public class LicenseHelper
{
    private readonly ICakeContext _context;
    
    // newtonsoft License file URL
    private const string NEWTONSOFT_LICENSE_URL = "https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md";

    // unity newtonsoft license file URL
    private const string UNITY_NEWTONSOFT_THIRD_PARTY_URL = "https://raw.githubusercontent.com/applejag/Newtonsoft.Json-for-Unity/master/THIRD-PARTY-NOTICES.md";
    private const string UNITY_NEWTONSOFT_LICENSE_URL = "https://raw.githubusercontent.com/applejag/Newtonsoft.Json-for-Unity/master/LICENSE.md";
    
    // msgpack license file URL
    private const string MSGPACK_LICENSE_URL = "https://raw.githubusercontent.com/MessagePack-CSharp/MessagePack-CSharp/v3.1.4/LICENSE";
    
    public LicenseHelper(ICakeContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Creates a temporary in-memory license file for .NET Framework dependencies
    /// </summary>
    /// <returns>FilePath to the temporary license file</returns>
    public FilePath GetNetFrameworkDependencyLicense()
    {
        _context.Information("Generating .NET Framework dependency licenses...");
        
        var content = new System.Text.StringBuilder();
        content.AppendLine("THIRD PARTY LICENSES");
        content.AppendLine("====================");
        content.AppendLine();
        content.AppendLine("This package includes the following third-party dependencies:");
        content.AppendLine();
        
        // MessagePack License
        content.AppendLine("1. MessagePack-CSharp");
        content.AppendLine("   License: MIT");
        content.AppendLine($"   Source: {MSGPACK_LICENSE_URL}");
        content.AppendLine();
        content.AppendLine(DownloadLicenseContent(MSGPACK_LICENSE_URL));
        content.AppendLine();
        content.AppendLine(new string('=', 80));
        content.AppendLine();
        
        // Newtonsoft.Json License
        content.AppendLine("2. Newtonsoft.Json");
        content.AppendLine("   License: MIT");
        content.AppendLine($"   Source: {NEWTONSOFT_LICENSE_URL}");
        content.AppendLine();
        content.AppendLine(DownloadLicenseContent(NEWTONSOFT_LICENSE_URL));
        content.AppendLine();
        
        return CreateTempLicenseFile(content.ToString());
    }
    
    /// <summary>
    /// Creates a temporary in-memory license file for Unity dependencies
    /// </summary>
    /// <returns>FilePath to the temporary license file</returns>
    public FilePath GetUnityDependencyLicenses()
    {
        _context.Information("Generating Unity dependency licenses...");
        
        var content = new System.Text.StringBuilder();
        content.AppendLine("THIRD PARTY LICENSES");
        content.AppendLine("====================");
        content.AppendLine();
        content.AppendLine("This package includes the following third-party dependencies:");
        content.AppendLine();
        
        // Unity Newtonsoft.Json Third Party Notices
        content.AppendLine("1. Newtonsoft.Json for Unity - Third Party Notices");
        content.AppendLine($"   Source: {UNITY_NEWTONSOFT_THIRD_PARTY_URL}");
        content.AppendLine();
        content.AppendLine(DownloadLicenseContent(UNITY_NEWTONSOFT_THIRD_PARTY_URL));
        content.AppendLine();
        content.AppendLine(new string('=', 80));
        content.AppendLine();
        
        // Unity Newtonsoft.Json License
        content.AppendLine("2. Newtonsoft.Json for Unity - License");
        content.AppendLine($"   Source: {UNITY_NEWTONSOFT_LICENSE_URL}");
        content.AppendLine();
        content.AppendLine(DownloadLicenseContent(UNITY_NEWTONSOFT_LICENSE_URL));
        content.AppendLine();
        content.AppendLine(new string('=', 80));
        content.AppendLine();
        
        // MessagePack License
        content.AppendLine("3. MessagePack-CSharp");
        content.AppendLine("   License: MIT");
        content.AppendLine($"   Source: {MSGPACK_LICENSE_URL}");
        content.AppendLine();
        content.AppendLine(DownloadLicenseContent(MSGPACK_LICENSE_URL));
        content.AppendLine();
        
        return CreateTempLicenseFile(content.ToString());
    }
    
    /// <summary>
    /// Downloads license content from a URL
    /// </summary>
    private string DownloadLicenseContent(string url)
    {
        try
        {
            _context.Information($"Downloading license from: {url}");
            
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
        }
        catch (Exception ex)
        {
            _context.Error($"Failed to download license from {url}: {ex.Message}");
            throw new Exception($"License download failed from {url}. Build cannot proceed without required license information.", ex);
        }
    }
    
    /// <summary>
    /// Creates a temporary file with the license content in the system temp directory.
    /// The file is overwritten on each call and will be cleaned up by the OS.
    /// </summary>
    private FilePath CreateTempLicenseFile(string content)
    {
        // Use filename without spaces to avoid ILRepack command-line parsing issues
        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "THIRD_PARTY_LICENSES.txt"
        );
        
        System.IO.File.WriteAllText(tempPath, content);
        _context.Information($"Created temporary license file: {tempPath}");
        
        return new FilePath(tempPath);
    }
}

var licenseHelper = new LicenseHelper(Context);
