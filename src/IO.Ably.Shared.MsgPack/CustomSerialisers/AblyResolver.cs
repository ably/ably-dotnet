using System;
using MessagePack;
using MessagePack.Formatters;
using IO.Ably.Types;

namespace IO.Ably.CustomSerialisers
{
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Custom resolver for Ably-specific types that require special serialization handling.
    /// </summary>
    public class AblyResolver : IFormatterResolver
    {
        public static readonly AblyResolver Instance = new AblyResolver();

        private AblyResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)GetFormatterHelper(typeof(T));
            }

            private static object GetFormatterHelper(Type t)
            {
                if (t == typeof(DateTimeOffset))
                {
                    return new DateTimeOffsetFormatter();
                }

                if (t == typeof(TimeSpan))
                {
                    return new TimespanFormatter();
                }

                if (t == typeof(Capability))
                {
                    return new CapabilityFormatter();
                }

                if (t == typeof(MessageExtras))
                {
                    return new MessageExtrasFormatter();
                }

                if (t == typeof(ChannelParams))
                {
                    return new ChannelParamsFormatter();
                }

                return null;
            }
        }
    }

#pragma warning restore SA1600 // Elements should be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
