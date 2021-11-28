using System;
using NUnit.Framework;

namespace IO.Ably.TestHelpers.Unity
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.False(false);
            // Assert.Pass();
            Console.WriteLine("Hello there");
            TestContext.Out.WriteLine("Hello there");
            TestContext.Error.WriteLine("Error occured");
            TestContext.Progress.WriteLine("Progress occured");
        }

        [Test]
        public void Test2()
        {
            Assert.True(true);
        }
    }
}