using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace IO.Ably.Tests
{
    public class ProtocolDataAttribute : DataAttribute
    {
        private readonly object[] _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolDataAttribute"/> class.
        /// </summary>
        /// <param name="data">The data values to pass to the theory.</param>
        public ProtocolDataAttribute(params object[] data)
        {
            _data = data;
        }

        /// <inheritdoc/>
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // Return Protocol.Json with relevant data
            var d = new List<object> { Protocol.Json };
            d.AddRange(_data);
            yield return d.ToArray();

            // Return Protocol.MsgPack with relevant data
            d = new List<object> { Protocol.MsgPack };
            d.AddRange(_data);
            yield return d.ToArray();
        }
    }
}
