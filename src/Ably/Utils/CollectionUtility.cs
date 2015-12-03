using RestSharp;
using System;
using System.Collections.Generic;
#if SILVERLIGHT
using SCS = Ably.Utils;
#else
using SCS = System.Collections.Specialized;
#endif

namespace Ably.Utils
{
    internal static class CollectionUtility
    {
        public static SCS.NameValueCollection ToNameValueCollection(IList<Parameter> collection)
        {
            SCS.NameValueCollection dict = new SCS.NameValueCollection();
            foreach (var item in collection)
            {
                dict.Add(item.Name, Convert.ToString(item.Value));
            }
            return dict;
        }
    }
}
