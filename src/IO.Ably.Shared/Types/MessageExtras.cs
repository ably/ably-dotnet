using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IO.Ably.Types
{
    /// <summary>
    /// Extra properties on the Message.
    /// </summary>
    public class MessageExtras
    {
        private const string DeltaProperty = "delta";

        [JsonIgnore]
        private JToken Data { get; }

        /// <summary>
        /// Delta extras is part of the Ably delta's functionality.
        /// </summary>
        public DeltaExtras Delta { get; }

        /// <summary>
        /// Messages extras is a flexible object that may other properties that are not exposed by the strongly typed implementation.
        /// </summary>
        /// <param name="data">the json object passed to Message extras.</param>
        public MessageExtras(JToken data = null)
        {
            Data = data;
            if (data != null && data is JObject dataObject)
            {
                var deltaProp = dataObject[DeltaProperty];
                if (deltaProp != null && deltaProp is JObject deltaObject)
                {
                    Delta = deltaObject.ToObject<DeltaExtras>();
                }
            }
        }

        /// <summary>
        /// Returns the inner message extras json object including any changes made to DeltaExtras.
        /// </summary>
        /// <returns>returns the inner json.</returns>
        public JToken ToJson()
        {
            if (Data == null && Delta == null)
            {
                return null;
            }

            var result = Data?.DeepClone() ?? new JObject();
            if (Delta != null)
            {
                result[DeltaProperty] = JObject.FromObject(Delta);
            }

            return result;
        }
    }

    /// <summary>
    /// Extra message properties relating to the delta's functionality.
    /// </summary>
    public class DeltaExtras
    {
        /// <summary>
        /// Format.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// From.
        /// </summary>
        public string From { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeltaExtras"/> class.
        /// </summary>
        /// <param name="from">from parameter.</param>
        /// <param name="format">format parameter.</param>
        public DeltaExtras(string from, string format)
        {
            From = from;
            Format = format;
        }
    }
}
