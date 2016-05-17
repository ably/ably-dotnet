using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace IO.Ably
{
    public class CapabilityResource
    {
        public static class AllowedOps
        {
            public const string Publish = "publish";
            public const string Subscribe = "subscribe";
            public const string Presence = "presence";
            public const string All = "*";
        }
        public string Name { get; set; }
        public List<string> AllowedOperations { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public CapabilityResource()
        {
            AllowedOperations = new List<string>();
        }

        public CapabilityResource(string name) : this()
        {
            Name = name;
        }

        public bool AllowsAll => AllowedOperations.Contains(AllowedOps.All);

        public void AllowAll()
        {
            AllowedOperations.Add(AllowedOps.All);
            AllowedOperations.Sort();
        }

        public CapabilityResource AllowPresence()
        {
            AllowedOperations.Add(AllowedOps.Presence);
            AllowedOperations.Sort();
            return this;
        }

        public CapabilityResource AllowSubscribe()
        {
            AllowedOperations.Add(AllowedOps.Subscribe);
            AllowedOperations.Sort();
            return this;
        }

        public CapabilityResource AllowPublish()
        {
            AllowedOperations.Add(AllowedOps.Publish);
            AllowedOperations.Sort();
            return this;
        }
    }
}