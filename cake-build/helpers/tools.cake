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
        
        // Get the root directory (parent of cake-build)
        var rootDir = _context.MakeAbsolute(_context.Directory("../"));
        var ilRepackPath = rootDir.CombineWithFilePath("tools/ilrepack.exe");
        
        // Use ILRepack directly like FAKE does
        var exitCode = _context.StartProcess(ilRepackPath.FullPath, new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .Append($"/lib:\"{sourcePath.FullPath}\"")
                .Append("/targetplatform:v4")
                .Append("/internalize")
                .Append($"/attr:\"{targetDll.FullPath}\"")
                .Append($"/keyfile:\"{rootDir.CombineWithFilePath("IO.Ably.snk").FullPath}\"")
                .Append("/parallel")
                .Append($"/out:\"{outputDll.FullPath}\"")
                .Append($"\"{targetDll.FullPath}\"")
                .Append($"\"{jsonNetDll.FullPath}\"")
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
    
    public void MergeDeltaCodec(DirectoryPath sourcePath, DirectoryPath outputPath)
    {
        var targetDll = outputPath.CombineWithFilePath("IO.Ably.dll");
        var docsFile = outputPath.CombineWithFilePath("IO.Ably.xml");
        var deltaCodecDll = sourcePath.CombineWithFilePath("IO.Ably.DeltaCodec.dll");
        var tempInputDll = outputPath.CombineWithFilePath("IO.Ably.temp.dll");
        
        if (!_context.FileExists(deltaCodecDll))
        {
            _context.Warning($"DeltaCodec DLL not found at {deltaCodecDll}, skipping merge...");
            return;
        }
        
        if (!_context.FileExists(targetDll))
        {
            _context.Warning($"Target DLL not found at {targetDll}, skipping merge...");
            return;
        }
        
        _context.Information($"Merging {deltaCodecDll.GetFilename()} into {targetDll.GetFilename()}...");
        
        // Get the root directory (parent of cake-build)
        var rootDir = _context.MakeAbsolute(_context.Directory("../"));
        var ilRepackPath = rootDir.CombineWithFilePath("tools/ilrepack.exe");
        
        // Copy target DLL to temp location to avoid input/output conflict
        _context.CopyFile(targetDll, tempInputDll);
        
        // Backup PDB and config files
        var targetPdb = outputPath.CombineWithFilePath("IO.Ably.pdb");
        var tempInputPdb = outputPath.CombineWithFilePath("IO.Ably.temp.pdb");
        if (_context.FileExists(targetPdb))
        {
            _context.CopyFile(targetPdb, tempInputPdb);
        }
        
        var targetConfig = outputPath.CombineWithFilePath("IO.Ably.dll.config");
        var tempInputConfig = outputPath.CombineWithFilePath("IO.Ably.temp.dll.config");
        if (_context.FileExists(targetConfig))
        {
            _context.CopyFile(targetConfig, tempInputConfig);
        }
        
        try
        {
            // Merge DeltaCodec into IO.Ably.dll (output directly with correct name)
            var exitCode = _context.StartProcess(ilRepackPath.FullPath, new ProcessSettings
            {
                Arguments = new ProcessArgumentBuilder()
                    .Append($"/lib:\"{sourcePath.FullPath}\"")
                    .Append($"/lib:\"{outputPath.FullPath}\"")
                    .Append("/targetplatform:v4")
                    .Append("/internalize")
                    .Append($"/attr:\"{tempInputDll.FullPath}\"")
                    .Append($"/keyfile:\"{rootDir.CombineWithFilePath("IO.Ably.snk").FullPath}\"")
                    .Append("/parallel")
                    .Append($"/out:\"{targetDll.FullPath}\"")
                    .Append($"\"{tempInputDll.FullPath}\"")
                    .Append($"\"{deltaCodecDll.FullPath}\"")
            });
            
            if (exitCode != 0)
            {
                throw new Exception($"ILRepack failed with exit code {exitCode}");
            }
        }
        finally
        {
            // Clean up temp files
            if (_context.FileExists(tempInputDll))
            {
                _context.DeleteFile(tempInputDll);
            }
            if (_context.FileExists(tempInputPdb))
            {
                _context.DeleteFile(tempInputPdb);
            }
            if (_context.FileExists(tempInputConfig))
            {
                _context.DeleteFile(tempInputConfig);
            }
        }
        
        // Clean up DeltaCodec files from output path since they're now merged
        var deltaCodecFiles = _context.GetFiles(outputPath.Combine("IO.Ably.DeltaCodec.*").FullPath);

        foreach (var file in deltaCodecFiles)
        {
            _context.DeleteFile(file);
            _context.Information($"Cleaned up: {file.GetFilename()}");
        }
        
        _context.Information($"✓ DeltaCodec merged into {targetDll}");
    }
}

var ilRepackHelper = new ILRepackHelper(Context);
