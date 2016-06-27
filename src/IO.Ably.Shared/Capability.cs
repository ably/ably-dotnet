using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IO.Ably
{
    /// <summary>
    /// Capability class that wraps the Ably capability string and provides a fluent interface in defining 
    /// capability objects
    /// <code>
    /// var capability = new Capability();
    ///
    /// capability.AddResource("second").AllowPublish();
    /// capability.AddResource("first").AllowAll();
    ///
    /// Assert.Equal("{ \"first\": [ \"*\" ], \"second\": [ \"publish\" ] }", capability.ToJson());
    /// </code>
    /// </summary>
    /// 
    
    public class Capability
    {
        /// <summary>
        /// Capability to allow all actions. This is the default passed to requests where the capability is not explicitly set
        /// </summary>
        public readonly static Capability AllowAll = new Capability("{ \"*\": [ \"*\" ] }");

        public List<CapabilityResource> Resources { get; set; }

        public Capability()
        {
            Resources = new List<CapabilityResource>();            
        }

        /// <summary>
        /// Creates a capability object by parsing an ably capability string
        /// </summary>
        /// <param name="capabilityString">Valid json capability string</param>
        public Capability(string capabilityString)
        {
            Resources = new List<CapabilityResource>();

            if (capabilityString.IsNotEmpty())
            {
                if (capabilityString.IsJson())
                {
                    var json = JObject.Parse(capabilityString);
                    foreach (var jToken in json.Children())
                    {
                        var child = (JProperty) jToken;
                        Resources.Add(GetResource(child));
                    }
                }
                else
                {
                    throw new ArgumentException("Capability string is not valid json", nameof(capabilityString));
                }
            }
        }

        private static CapabilityResource GetResource(JProperty child)
        {
            var resource = new CapabilityResource(child.Name);
            var allowedOperations = child.Value as JArray;
            if (allowedOperations != null)
                foreach (JToken token in allowedOperations)
                    resource.AllowedOperations.Add((string)token);
            return resource;
        }


        /// <summary>
        /// Adds a capability resource. The resource returned can be used to define the actions allowed for it by chaining the Allow methods
        /// Possible options are: AllowAll, AllowPublish, AllowPresence and AllowSubscribe
        /// A Resource can be a channel "channel" or a namespace "namespace:*". Please consult the rest documentation
        /// </summary>
        /// <code>
        /// var capability = new Capability();
        ///
        /// capability.AddResource("name").AllowPublish();
        /// </code>
        /// <param name="name">name of the resource</param>
        /// <returns>CapabilityResource</returns>
        public CapabilityResource AddResource(string name)
        {
            var resource = new CapabilityResource(name);
            Resources.Add(resource);

            return resource;
        }

        /// <summary>
        /// Returns the Ably capability json based on the current object.
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            var result = new JObject();
            var orderedResources = Resources.OrderBy(x => x.Name).Where(x => x.AllowedOperations.Any());
            foreach (var resource in orderedResources)
            {
                result[resource.Name] = GetResourceValue(resource);
            }
            if(result.Children().Any())
                return CleanUpWhiteSpace(result.ToString());
            return "";
        }

        public override string ToString()
        {
            return ToJson();
        }

        private static JArray GetResourceValue(CapabilityResource resource)
        {
            if (resource.AllowsAll)
                return new JArray(CapabilityResource.AllowedOps.All);
            if (resource.AllowedOperations.Count == 1)
                return new JArray(resource.AllowedOperations.First());
            return new JArray(resource.AllowedOperations.ToArray());
        }

        private string CleanUpWhiteSpace(string jsonString)
        {
            return Regex.Replace(jsonString, @"\s+", " ", RegexOptions.Singleline);
        }

        protected bool Equals(Capability other)
        {
            return ToJson().Equals(other.ToJson());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Capability) obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

}
