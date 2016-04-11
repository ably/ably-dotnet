using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace IO.Ably.Tests
{
    public class ProtocolDataAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return new object[] {Protocol.Json};
            yield return new object[] { Protocol.MsgPack};
        }
    }
}