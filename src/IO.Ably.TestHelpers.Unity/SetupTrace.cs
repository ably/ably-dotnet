using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace IO.Ably.TestHelpers.Unity
{
    [SetUpFixture]
    public class SetupTrace
    {
        [OneTimeSetUp]
        public void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [OneTimeTearDown]
        public void EndTest()
        {
            Trace.Flush();
        }
    }
    //
    // [Test]
    // public void Test1()
    // {
    // Debug.WriteLine("This is Debug.WriteLine");
    // Trace.WriteLine("This is Trace.WriteLine");
    // Console.WriteLine("This is Console.Writeline");
    // TestContext.WriteLine("This is TestContext.WriteLine");
    // TestContext.Out.WriteLine("This is TestContext.Out.WriteLine");
    // TestContext.Progress.WriteLine("This is TestContext.Progress.WriteLine");
    // TestContext.Error.WriteLine("This is TestContext.Error.WriteLine");
    // Assert.Pass();
    // }
}
