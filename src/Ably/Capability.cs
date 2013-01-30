using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ably
{
    public class Capability
    {
        public List<CapabilityResource> Resources { get; set; }

        public Capability()
        {
            Resources = new List<CapabilityResource>();            
        }

        public Capability(string capabilityString)
        {
            Resources = new List<CapabilityResource>();

            var json = JObject.Parse(capabilityString);
            foreach(JProperty child in json.Children())
            {
                Resources.Add(GetResource(child));
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

        public CapabilityResource AddResource(string name)
        {
            var resource = new CapabilityResource(name);
            Resources.Add(resource);

            return resource;
        }

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

        private static JArray GetResourceValue(CapabilityResource resource)
        {
            if (resource.AllowsAll)
                return new JArray(CapabilityResource.AllowedOps.All);
            else if (resource.AllowedOperations.Count == 1)
                return new JArray(resource.AllowedOperations.First());
            else
                return new JArray(resource.AllowedOperations.ToArray());
        }

        private string CleanUpWhiteSpace(string jsonString)
        {
            return Regex.Replace(jsonString, @"\s+", " ", RegexOptions.Singleline);
        }
    }

}
