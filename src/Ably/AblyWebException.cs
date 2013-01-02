using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Ably
{
    public class AblyWebException : AblyException
    {
        /// <summary>
        /// Initializes a new instance of the AblyException class.
        /// </summary>
        public AblyWebException(HttpWebResponse response)
        {

        }

        public HttpStatusCode HttpStatusCode { get; set; }
        public string ErrorCode { get; set; }
        public string Reason { get; set; }
        public string StatusCode { get; set; }


    }
}
