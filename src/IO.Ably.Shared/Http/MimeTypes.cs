using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace IO.Ably
{
    internal class MimeTypes : Dictionary<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the MimeTypes class.
        /// </summary>
        public MimeTypes()
        {
            Add("json", "");
            Add("binary", "application/x-msgpack");
        }

        public string GetHeaderValue(params string[] keys)
        {
            return string.Join(",", this.Where(x => keys.Contains(x.Key)).Select(x => x.Value));
        }
    }
}
