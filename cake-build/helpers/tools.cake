///////////////////////////////////////////////////////////////////////////////
// TOOL HELPERS
///////////////////////////////////////////////////////////////////////////////

public class ILRepackHelper
{
    private readonly ICakeContext _context;
    
    public ILRepackHelper(ICakeContext context)
    {
        _context = context;
    }
    
    public void MergeJsonNet(DirectoryPath sourcePath, DirectoryPath outputPath)
    {
        var targetDll = sourcePath.CombineWithFilePath("IO.Ably.dll");
        var docsFile = sourcePath.CombineWithFilePath("IO.Ably.xml");
        var outputDll = outputPath.CombineWithFilePath("IO.Ably.dll");
        var jsonNetDll = sourcePath.CombineWithFilePath("Newtonsoft.Json.dll");
        
        _context.EnsureDirectoryExists(outputPath);
        
        _context.Information($"Merging {jsonNetDll.GetFilename()} into {targetDll.GetFilename()}...");
        
        // Use ILRepack directly like FAKE does
        var exitCode = _context.StartProcess("./tools/ilrepack.exe", new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .Append($"/lib:{sourcePath.FullPath}")
                .Append("/targetplatform:v4")
                .Append("/internalize")
                .Append($"/attr:{targetDll.FullPath}")
                .Append("/keyfile:IO.Ably.snk")
                .Append("/parallel")
                .Append($"/out:{outputDll.FullPath}")
                .Append(targetDll.FullPath)
                .Append(jsonNetDll.FullPath)
        });
        
        if (exitCode != 0)
        {
            throw new Exception($"ILRepack failed with exit code {exitCode}");
        }
        
        // Copy XML documentation
        if (_context.FileExists(docsFile))
        {
            _context.CopyFile(docsFile, outputPath.CombineWithFilePath("IO.Ably.xml"));
        }
        
        _context.Information($"✓ Merged assembly created at {outputDll}");
    }
}

var ilRepackHelper = new ILRepackHelper(Context);
