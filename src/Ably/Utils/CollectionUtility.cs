using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Ably.Utils
{
    internal static class CollectionUtility
    {
        public static NameValueCollection ToNameValueCollection(IList<Parameter> collection)
        {
            NameValueCollection dict = new NameValueCollection();
            foreach (var item in collection)
            {
                dict.Add(item.Name, Convert.ToString(item.Value));
            }
            return dict;
        }
    }
}
