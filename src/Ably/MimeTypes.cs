using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Ably
{
    internal class MimeTypes : Dictionary<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the MimeTypes class.
        /// </summary>
        public MimeTypes()
        {
            Add("json", "application/json");
            Add("xml", "application/xml");
            Add("html", "text/html");
            Add("binary", "application/x-thrift");
        }

        public string GetHeaderValue(params string[] keys)
        {
            return string.Join(",", this.Where(x => keys.Contains(x.Key)).Select(x => x.Value));
        }
    }
}
