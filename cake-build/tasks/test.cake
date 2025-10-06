///////////////////////////////////////////////////////////////////////////////
// TEST TASKS - NETFRAMEWORK
///////////////////////////////////////////////////////////////////////////////

Task("NetFramework-Unit-Tests")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework unit tests...");
    
    var testAssemblies = testExecutionHelper.FindTestAssemblies("IO.Ably.Tests.NETFramework");
    if (!testAssemblies.Any()) return;
    
    var settings = testExecutionHelper.CreateXUnitSettings("xunit-netframework-unit", isIntegration: false);
    testExecutionHelper.RunXUnitTests(testAssemblies, settings);
});

Task("NetFramework-Unit-Tests-WithRetry")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework unit tests with retry...");
    
    var testAssemblies = testExecutionHelper.FindTestAssemblies("IO.Ably.Tests.NETFramework");
    if (!testAssemblies.Any()) return;
    
    var resultsPath = paths.TestResults.CombineWithFilePath("xunit-netframework-unit.xml");
    
    try
    {
        var settings = testExecutionHelper.CreateXUnitSettings("xunit-netframework-unit", isIntegration: false);
        testExecutionHelper.RunXUnitTests(testAssemblies, settings);
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    testExecutionHelper.RetryFailedXUnitTests(
        testAssemblies, 
        resultsPath,
        testRetryHelper,
        (test) => testExecutionHelper.CreateXUnitSettings("retry", isIntegration: false, isRetry: true)
    );
});

Task("NetFramework-Integration-Tests")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework integration tests...");
    
    var testAssemblies = testExecutionHelper.FindTestAssemblies("IO.Ably.Tests.NETFramework");
    if (!testAssemblies.Any()) return;
    
    var settings = testExecutionHelper.CreateXUnitSettings("xunit-netframework-integration", isIntegration: true);
    testExecutionHelper.RunXUnitTests(testAssemblies, settings);
});

Task("NetFramework-Integration-Tests-WithRetry")
    .IsDependentOn("NetFramework-Build")
    .Does(() =>
{
    Information("Running .NET Framework integration tests with retry...");
    
    var testAssemblies = testExecutionHelper.FindTestAssemblies("IO.Ably.Tests.NETFramework");
    if (!testAssemblies.Any()) return;
    
    var resultsPath = paths.TestResults.CombineWithFilePath("xunit-netframework-integration.xml");
    
    try
    {
        var settings = testExecutionHelper.CreateXUnitSettings("xunit-netframework-integration", isIntegration: true);
        testExecutionHelper.RunXUnitTests(testAssemblies, settings);
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    testExecutionHelper.RetryFailedXUnitTests(
        testAssemblies, 
        resultsPath,
        testRetryHelper,
        (test) => testExecutionHelper.CreateXUnitSettings("retry", isIntegration: false, isRetry: true)
    );
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
    
    var filter = testExecutionHelper.CreateUnitTestFilter(IsRunningOnUnix());
    var settings = testExecutionHelper.CreateDotNetTestSettings(resultsPath, filter, framework, configuration);
    
    testExecutionHelper.RunDotNetTests(project, settings);
});

Task("NetStandard-Unit-Tests-WithRetry")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard unit tests with retry...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-unit.trx");
    
    var filter = testExecutionHelper.CreateUnitTestFilter(IsRunningOnUnix());
    var settings = testExecutionHelper.CreateDotNetTestSettings(resultsPath, filter, framework, configuration);
    
    try
    {
        testExecutionHelper.RunDotNetTests(project, settings);
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    testExecutionHelper.RetryFailedDotNetTests(project, resultsPath, testRetryHelper, framework, configuration);
});

Task("NetStandard-Integration-Tests")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard integration tests...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-integration.trx");
    
    var filter = testExecutionHelper.CreateIntegrationTestFilter();
    var settings = testExecutionHelper.CreateDotNetTestSettings(resultsPath, filter, framework, configuration);
    
    testExecutionHelper.RunDotNetTests(project, settings);
});

Task("NetStandard-Integration-Tests-WithRetry")
    .IsDependentOn("NetStandard-Build")
    .Does(() =>
{
    Information("Running .NET Standard integration tests with retry...");
    
    var project = paths.Src.CombineWithFilePath("IO.Ably.Tests.DotNET/IO.Ably.Tests.DotNET.csproj");
    var resultsPath = paths.TestResults.CombineWithFilePath("tests-netstandard-integration.trx");
    
    var filter = testExecutionHelper.CreateIntegrationTestFilter();
    var settings = testExecutionHelper.CreateDotNetTestSettings(resultsPath, filter, framework, configuration);
    
    try
    {
        testExecutionHelper.RunDotNetTests(project, settings);
    }
    catch
    {
        Warning("Some tests failed. Retrying failed tests...");
    }
    
    testExecutionHelper.RetryFailedDotNetTests(project, resultsPath, testRetryHelper, framework, configuration);
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
