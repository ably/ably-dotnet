using MessagePack.Resolvers;
using MessagePack;
using System.Collections.Generic;

namespace IO.Ably.Tests.Shared.MsgPack
{
    internal class MsgPackTestExtensions
    {
        internal static MessagePackSerializerOptions GetTestOptions()
        {
            var testResolvers = new List<IFormatterResolver>(MsgPackHelper.Resolvers)
            {
                StandardResolver.Instance
            };
            return MessagePackSerializerOptions.Standard.WithResolver(CompositeResolver.Create(testResolvers.ToArray()));
        }
    }
}
