///////////////////////////////////////////////////////////////////////////////
// DEPENDENCY HELPERS
///////////////////////////////////////////////////////////////////////////////

public class Deps
{
    private readonly ICakeContext _context;
    private readonly BuildPaths _paths;
    
    public Deps(ICakeContext context, BuildPaths paths)
    {
        _context = context;
        _paths = paths;
    }

    // Common DLLs used across multiple build configurations
    private readonly List<string> _commonDlls = new List<string>
    {
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Threading.Channels.dll",
        "System.Threading.Tasks.Extensions.dll",
        "IO.Ably.DeltaCodec.dll"
    };
    
    // MessagePack related DLLs
    private readonly List<string> _msgpackDlls = new List<string>
    {
        "MessagePack.dll",
        "MessagePack.Annotations.dll",
        "System.Memory.dll",
        "System.Buffers.dll",
        "Microsoft.Bcl.AsyncInterfaces.dll",
        "System.Collections.Immutable.dll",
        "Microsoft.NET.StringTools.dll"
    };

    private readonly string _newtonsoftDll = "Newtonsoft.Json.dll";

    // Instance property that accesses the paths instance
    private FilePath UnityNewtonsoftDll => _paths.Root
        .Combine("lib/unity/AOT")
        .CombineWithFilePath("_newtonsoftDll");
    
    /// <summary>
    /// Gets all Unity package dependencies (common + msgpack DLLs) combined with the base path
    /// </summary>
    /// <param name="basePath">The base directory path to combine with each DLL filename</param>
    /// <returns>List of FilePath objects for all dependencies including Newtonsoft.Json</returns>
    public List<FilePath> GetUnityPackageDependencies(DirectoryPath basePath)
    {
        var dependencies = new List<FilePath>();
        
        // Add Unity Newtonsoft.Json DLL first
        CheckDependency(UnityNewtonsoftDll);
        dependencies.Add(UnityNewtonsoftDll);
        
        // Add all common DLLs
        foreach (var dll in _commonDlls)
        {
            var filePath = basePath.CombineWithFilePath(dll);
            CheckDependency(filePath);
            dependencies.Add(filePath);
        }
        
        // Add all msgpack DLLs
        foreach (var dll in _msgpackDlls)
        {
            var filePath = basePath.CombineWithFilePath(dll);
            CheckDependency(filePath);
            dependencies.Add(filePath);
        }
        
        return dependencies;
    }
    
    /// <summary>
    /// Checks if a dependency file exists at the given path
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <exception cref="Exception">Thrown when the dependency file is not found</exception>
    private void CheckDependency(FilePath filePath)
    {
        if (!_context.FileExists(filePath))
        {
            throw new Exception($"Dependency not found: {filePath.FullPath}");
        }
    }
}

var deps = new Deps(Context, paths);
