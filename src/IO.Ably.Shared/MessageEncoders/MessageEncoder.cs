using System;
using System.Linq;
using IO.Ably.Rest;

namespace IO.Ably.MessageEncoders
{
    internal abstract class MessageEncoder
    {
        public class ProcessedPayload : IPayload
        {
            public object Data { get; set; }

            public string Encoding { get; set; }

            public ProcessedPayload()
            {
            }

            public ProcessedPayload(IPayload payload)
            {
                if (payload != null)
                {
                    Data = payload.Data;
                    Encoding = payload.Encoding;
                }
            }
        }

        public abstract string EncodingName { get; }

        public abstract bool CanProcess(string currentEncoding);

        public abstract Result<ProcessedPayload> Encode(IPayload payload, DecodingContext context);

        public abstract Result<ProcessedPayload> Decode(IPayload payload, DecodingContext context);

        public bool IsEmpty(object data)
        {
            return data == null || (data is string s && s.IsEmpty());
        }

        public static string AddEncoding(IPayload payload, string encoding)
        {
            var encodingToAdd = encoding;
            if (payload.Encoding.IsEmpty())
            {
                return encodingToAdd;
            }

            return payload.Encoding + "/" + encodingToAdd;
        }

        public static bool CurrentEncodingIs(IPayload payload, string encoding)
        {
            return payload.Encoding.IsNotEmpty() && payload.Encoding.EndsWith(encoding, StringComparison.CurrentCultureIgnoreCase);
        }

        public static string GetCurrentEncoding(IPayload payload)
        {
            if (payload.Encoding.IsEmpty())
            {
                return string.Empty;
            }

            return payload.Encoding.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        public static string RemoveCurrentEncodingPart(IPayload payload)
        {
            if (payload.Encoding.IsEmpty())
            {
                return string.Empty;
            }

            var encodings = payload.Encoding.Split(new[] { '/' });
            return string.Join("/", encodings.Take(encodings.Length - 1));
        }
    }
}
