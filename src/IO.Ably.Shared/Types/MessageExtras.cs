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
        private JToken Data { get; set; }

        /// <summary>
        /// Delta extras is part of the Ably delta's functionality.
        /// </summary>
        public DeltaExtras Delta { get; set; }

        /// <summary>
        /// Messages extras is a flexible object that may other properties that are not exposed by the strongly typed implementation.
        /// </summary>
        /// <param name="data">the json object passed to Message extras.</param>
        public MessageExtras(JToken data = null)
        {
            Data = data;
            ParseData();
        }

        private void ParseData()
        {
            if (Data != null && Data is JObject dataObject)
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
            var result = Data?.DeepClone();
            if (result != null && Delta != null)
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
        public string Format { get; set; }

        /// <summary>
        /// From.
        /// </summary>
        public string From { get; set; }
    }
}
