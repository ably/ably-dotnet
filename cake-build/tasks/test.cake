///////////////////////////////////////////////////////////////////////////////
// TEST TASKS - NETFRAMEWORK
///////////////////////////////////////////////////////////////////////////////

Task("NetFramework-Unit-Tests")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework unit tests...");
    
    var testAssemblies = GetFiles("src/IO.Ably.Tests.NETFramework/bin/Release/*.Tests.*.dll");
    
    if (!testAssemblies.Any())
    {
        Warning("No test assemblies found for .NET Framework");
        return;
    }
    
    XUnit2(testAssemblies, new XUnit2Settings
    {
        OutputDirectory = paths.TestResults,
        XmlReport = true,
        ReportName = "xunit-netframework-unit",
        ArgumentCustomization = args => args
            .Append("-notrait")
            .AppendQuoted("type=integration")
    });
});

Task("NetFramework-Unit-Tests-WithRetry")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework unit tests with retry...");
    
    var testAssemblies = GetFiles("src/IO.Ably.Tests.NETFramework/bin/Release/*.Tests.*.dll");
    var resultsPath = paths.TestResults.CombineWithFilePath("xunit-netframework-unit.xml");
    
    if (!testAssemblies.Any())
    {
        Warning("No test assemblies found for .NET Framework");
        return;
    }
    
    try
    {
        XUnit2(testAssemblies, new XUnit2Settings
        {
            OutputDirectory = paths.TestResults,
            XmlReport = true,
            ReportName = "xunit-netframework-unit",
            ArgumentCustomization = args => args
                .Append("-notrait")
                .AppendQuoted("type=integration")
        });
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    var failedTests = testRetryHelper.FindFailedXUnitTests(resultsPath);
    Information($"Found {failedTests.Count} failed tests to retry");
    
    foreach (var test in failedTests)
    {
        Information($"Retrying test: {test}");
        
        var retryResultsPath = testRetryHelper.GetNextTestResultPath(resultsPath);
        
        try
        {
            XUnit2(testAssemblies, new XUnit2Settings
            {
                OutputDirectory = paths.TestResults,
                XmlReport = true,
                ReportName = retryResultsPath.GetFilenameWithoutExtension().FullPath,
                ArgumentCustomization = args => args
                    .Append("-method")
                    .AppendQuoted(test)
            });
        }
        catch
        {
            Warning($"Test {test} failed on retry");
        }
    }
});

Task("NetFramework-Integration-Tests")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework integration tests...");
    
    var testAssemblies = GetFiles("src/IO.Ably.Tests.NETFramework/bin/Release/*.Tests.*.dll");
    
    if (!testAssemblies.Any())
    {
        Warning("No test assemblies found for .NET Framework");
        return;
    }
    
    XUnit2(testAssemblies, new XUnit2Settings
    {
        OutputDirectory = paths.TestResults,
        XmlReport = true,
        ReportName = "xunit-netframework-integration",
        MaxThreads = 0,
        Parallelism = ParallelismOption.Collections,
        ArgumentCustomization = args => args
            .Append("-trait")
            .AppendQuoted("type=integration")
            .Append("-maxthreads")
            .Append("0")
    });
});

Task("NetFramework-Integration-Tests-WithRetry")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework integration tests with retry...");
    
    var testAssemblies = GetFiles("src/IO.Ably.Tests.NETFramework/bin/Release/*.Tests.*.dll");
    var resultsPath = paths.TestResults.CombineWithFilePath("xunit-netframework-integration.xml");
    
    if (!testAssemblies.Any())
    {
        Warning("No test assemblies found for .NET Framework");
        return;
    }
    
    try
    {
        XUnit2(testAssemblies, new XUnit2Settings
        {
            OutputDirectory = paths.TestResults,
            XmlReport = true,
            ReportName = "xunit-netframework-integration",
            MaxThreads = 0,
            Parallelism = ParallelismOption.Collections,
            ArgumentCustomization = args => args
                .Append("-trait")
                .AppendQuoted("type=integration")
                .Append("-maxthreads")
                .Append("0")
        });
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    var failedTests = testRetryHelper.FindFailedXUnitTests(resultsPath);
    Information($"Found {failedTests.Count} failed tests to retry");
    
    foreach (var test in failedTests)
    {
        Information($"Retrying test: {test}");
        
        var retryResultsPath = testRetryHelper.GetNextTestResultPath(resultsPath);
        
        try
        {
            XUnit2(testAssemblies, new XUnit2Settings
            {
                OutputDirectory = paths.TestResults,
                XmlReport = true,
                ReportName = retryResultsPath.GetFilenameWithoutExtension().FullPath,
                ArgumentCustomization = args => args
                    .Append("-method")
                    .AppendQuoted(test)
            });
        }
        catch
        {
            Warning($"Test {test} failed on retry");
        }
    }
});

///////////////////////////////////////////////////////////////////////////////
// TEST TASKS - NETSTANDARD
///////////////////////////////////////////////////////////////////////////////

Task("NetStandard-Unit-Tests")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard unit tests...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-unit.trx");
    
    if (!FileExists(project))
    {
        Warning($"Test project not found: {project}");
        return;
    }
    
    var filters = new List<string> { "type!=integration" };
    
    if (IsRunningOnUnix())
    {
        filters.Add("linux!=skip");
    }
    
    var settings = new DotNetTestSettings
    {
        Configuration = configuration,
        Filter = string.Join("&", filters),
        Loggers = new[] { $"trx;logfilename={resultsPath.FullPath}" },
        NoBuild = true,
        NoRestore = true
    };
    
    if (!string.IsNullOrEmpty(framework))
    {
        settings.Framework = framework;
    }
    
    DotNetTest(project.FullPath, settings);
});

Task("NetStandard-Unit-Tests-WithRetry")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard unit tests with retry...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-unit.trx");
    
    if (!FileExists(project))
    {
        Warning($"Test project not found: {project}");
        return;
    }
    
    var filters = new List<string> { "type!=integration" };
    
    if (IsRunningOnUnix())
    {
        filters.Add("linux!=skip");
    }
    
    var settings = new DotNetTestSettings
    {
        Configuration = configuration,
        Filter = string.Join("&", filters),
        Loggers = new[] { $"trx;logfilename={resultsPath.FullPath}" },
        NoBuild = true,
        NoRestore = true
    };
    
    if (!string.IsNullOrEmpty(framework))
    {
        settings.Framework = framework;
    }
    
    try
    {
        DotNetTest(project.FullPath, settings);
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    var failedTests = testRetryHelper.FindFailedDotNetTests(resultsPath);
    Information($"Found {failedTests.Count} failed tests to retry");
    
    foreach (var test in failedTests)
    {
        Information($"Retrying test: {test}");
        
        var retryResultsPath = testRetryHelper.GetNextTestResultPath(resultsPath, ".trx");
        
        var retrySettings = new DotNetTestSettings
        {
            Configuration = configuration,
            Filter = test,
            Loggers = new[] { $"trx;logfilename={retryResultsPath.FullPath}" },
            NoBuild = true,
            NoRestore = true
        };
        
        if (!string.IsNullOrEmpty(framework))
        {
            retrySettings.Framework = framework;
        }
        
        try
        {
            DotNetTest(project.FullPath, retrySettings);
        }
        catch
        {
            Warning($"Test {test} failed on retry");
        }
    }
});

Task("NetStandard-Integration-Tests")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard integration tests...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-integration.trx");
    
    if (!FileExists(project))
    {
        Warning($"Test project not found: {project}");
        return;
    }
    
    var settings = new DotNetTestSettings
    {
        Configuration = configuration,
        Filter = "type=integration",
        Loggers = new[] { $"trx;logfilename={resultsPath.FullPath}" },
        NoBuild = true,
        NoRestore = true
    };
    
    if (!string.IsNullOrEmpty(framework))
    {
        settings.Framework = framework;
    }
    
    DotNetTest(project.FullPath, settings);
});

Task("NetStandard-Integration-Tests-WithRetry")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard integration tests with retry...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-integration.trx");
    
    if (!FileExists(project))
    {
        Warning($"Test project not found: {project}");
        return;
    }
    
    var settings = new DotNetTestSettings
    {
        Configuration = configuration,
        Filter = "type=integration",
        Loggers = new[] { $"trx;logfilename={resultsPath.FullPath}" },
        NoBuild = true,
        NoRestore = true
    };
    
    if (!string.IsNullOrEmpty(framework))
    {
        settings.Framework = framework;
    }
    
    try
    {
        DotNetTest(project.FullPath, settings);
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    var failedTests = testRetryHelper.FindFailedDotNetTests(resultsPath);
    Information($"Found {failedTests.Count} failed tests to retry");
    
    foreach (var test in failedTests)
    {
        Information($"Retrying test: {test}");
        
        var retryResultsPath = testRetryHelper.GetNextTestResultPath(resultsPath, ".trx");
        
        var retrySettings = new DotNetTestSettings
        {
            Configuration = configuration,
            Filter = test,
            Loggers = new[] { $"trx;logfilename={retryResultsPath.FullPath}" },
            NoBuild = true,
            NoRestore = true
        };
        
        if (!string.IsNullOrEmpty(framework))
        {
            retrySettings.Framework = framework;
        }
        
        try
        {
            DotNetTest(project.FullPath, retrySettings);
        }
        catch
        {
            Warning($"Test {test} failed on retry");
        }
    }
});

///////////////////////////////////////////////////////////////////////////////
// PUBLIC TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Test.NetFramework.Unit")
    .IsDependentOn("NetFramework-Unit-Tests");

Task("Test.NetFramework.Unit.WithRetry")
    .IsDependentOn("NetFramework-Unit-Tests-WithRetry");

Task("Test.NetFramework.Integration")
    .IsDependentOn("NetFramework-Integration-Tests");

Task("Test.NetFramework.Integration.WithRetry")
    .IsDependentOn("NetFramework-Integration-Tests-WithRetry");

Task("Test.NetStandard.Unit")
    .IsDependentOn("NetStandard-Unit-Tests");

Task("Test.NetStandard.Unit.WithRetry")
    .IsDependentOn("NetStandard-Unit-Tests-WithRetry");

Task("Test.NetStandard.Integration")
    .IsDependentOn("NetStandard-Integration-Tests");

Task("Test.NetStandard.Integration.WithRetry")
    .IsDependentOn("NetStandard-Integration-Tests-WithRetry");
