using System;
using System.Collections.Generic;
using System.Linq;

namespace Ably
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
        public ISet<string> AllowedOperations { get; set; }

        public CapabilityResource(string name)
        {
            Name = name;
#if SILVERLIGHT
            AllowedOperations = new HashSet<string>();
#else
            AllowedOperations = new SortedSet<string>();
#endif
        }

        public bool AllowsAll
        {
            get { return AllowedOperations.Contains(AllowedOps.All); }
        }

        public void AllowAll()
        {
            AllowedOperations.Add(AllowedOps.All);
        }

        public CapabilityResource AllowPresence()
        {
            AllowedOperations.Add(AllowedOps.Presence);
            return this;
        }

        public CapabilityResource AllowSubscribe()
        {
            AllowedOperations.Add(AllowedOps.Subscribe);
            return this;
        }

        public CapabilityResource AllowPublish()
        {
            AllowedOperations.Add(AllowedOps.Publish);
            return this;
        }
    }
}
