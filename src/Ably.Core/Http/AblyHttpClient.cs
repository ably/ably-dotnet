using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        internal static readonly MimeTypes MimeTypes = new MimeTypes();

        private string _host;
        private readonly int? _port;
        private readonly bool _isSecure;
        private readonly AblyEnvironment? _environment;
        private bool _isDefaultHost;

        public AblyHttpClient(string host) : this(host, null, true) { }

        public AblyHttpClient(string host, int? port = null, bool isSecure = true, AblyEnvironment? environment = AblyEnvironment.Live)
        {
            _isSecure = isSecure;
            _environment = environment;
            _port = port;
            _host = host;
            _isDefaultHost = host == Config.DefaultHost;
        }

        public AblyResponse Execute(AblyRequest request)
        {
            var hosts = Config.FallbackHosts;

            int currentTry = 0;
            var startTime = Config.Now();
            while (currentTry <= hosts.Length)
            {
                var requestTime = Config.Now();
                if ((requestTime - startTime).TotalSeconds > Config.CommulativeFailedRequestTimeOutInSeconds)
                {
                    Logger.Error("Cumulative retry timeout of {0}s was exceeded", Config.CommulativeFailedRequestTimeOutInSeconds);
                    throw new AblyException(
                        new ErrorInfo(string.Format("Commulative retry timeout of {0}s was exceeded.",
                            Config.CommulativeFailedRequestTimeOutInSeconds), 500, null));
                }

                try
                {
                    return ExecuteInternal(request);
                }
                catch (WebException exception)
                {
                    var errorResponse = exception.Response as HttpWebResponse;

                    if (IsRetryable(exception) && _isDefaultHost)
                    {
                        Logger.Error("Error making a connection to Ably servers. Retrying", exception);
                        _host = hosts[currentTry - 1];
                        currentTry++;
                        continue;
                    }

                    if( errorResponse != null )
                    {
                        AblyResponse ablyResponse = GetAblyResponse( errorResponse );
                        Logger.ErrorResponse( ablyResponse );
                        throw AblyException.FromResponse( ablyResponse );
                    }

                    throw new AblyException(new ErrorInfo("Unexpected error. Check the inner exception for details", 500, null), exception);
                }
            }
            throw new AblyException(new ErrorInfo("Unexpected error while making a request.", 500, null));
        }

        private bool IsRetryable(WebException ex)
        {
            return ex.Status == WebExceptionStatus.ConnectFailure || ex.Status == WebExceptionStatus.SendFailure;
        }

        private AblyResponse ExecuteInternal(AblyRequest request)
        {
            Func<Task<AblyResponse>> fn = () =>execImpl( request );
            try
            {
                return Task.Run( fn ).Result;
            }
            catch( AggregateException aex )
            {
                Exception ex = aex.Flatten().InnerExceptions.First();
                throw ex;
            }

            /* var webRequest = HttpWebRequest.Create(GetRequestUrl(request)) as HttpWebRequest;
            webRequest.Timeout = Config.ConnectTimeout;
            HttpWebResponse response = null;

            webRequest.Headers[ "X-Ably-Version" ] = Config.AblyVersion;
            PopulateDefaultHeaders(request, webRequest);
            PopulateWebRequestHeaders(webRequest, request.Headers);

            webRequest.UserAgent = "Ably.net library";
            webRequest.Method = request.Method.Method;

            try
            {
                if (webRequest.Method == "POST")
                {
                    var requestBody = request.RequestBody;

                    webRequest
                        .ContentLength = requestBody.Length;
                    if (requestBody.Any())
                    {
                        using (Stream stream = webRequest.GetRequestStream())
                        {
                            stream.Write(requestBody, 0, requestBody.Length);
                        }
                    }
                }

                response = webRequest.GetResponse() as HttpWebResponse;
                return GetAblyResponse(response);
            }
            finally
            {
                if (response != null)
                    response.Close();
            } */
        }

        static async Task withTimeout( Task useful, Task timeout )
        {
            if( timeout == await Task.WhenAny( useful, timeout ) )
                throw new TimeoutException();
        }

        static async Task<T> withTimeout<T>( Task<T> useful, Task timeout )
        {
            if( timeout == await Task.WhenAny( useful, timeout ) )
                throw new TimeoutException();
            return useful.Result;
        }

        async Task<AblyResponse> execImpl( AblyRequest request )
        {
            var webRequest = HttpWebRequest.Create(GetRequestUrl(request)) as HttpWebRequest;
            HttpWebResponse response = null;
            Task tTimeout = Task.Delay( Config.ConnectTimeout );

            webRequest.Headers[ "X-Ably-Version" ] = Config.AblyVersion;
            PopulateDefaultHeaders( request, webRequest );
            PopulateWebRequestHeaders( webRequest, request.Headers );

            webRequest.Method = request.Method.Method;

            try
            {
                if( webRequest.Method == "POST" )
                {
                    var requestBody = request.RequestBody;

                    using( Stream stream = await withTimeout( webRequest.GetRequestStreamAsync(), tTimeout ) )
                    {
                        // Need GetRequestStreamAsync() to have 0 ContentLength
                        // http://stackoverflow.com/a/13692598/126995
                        if( requestBody.Length > 0 )
                            await withTimeout( stream.WriteAsync( requestBody, 0, requestBody.Length ), tTimeout );
                    }
                }

                response = ( HttpWebResponse ) await withTimeout( webRequest.GetResponseAsync(), tTimeout );
                return GetAblyResponse( response );
            }
            finally
            {
                if( response != null )
                    response.Dispose();
            }
        }

        private void PopulateDefaultHeaders(AblyRequest request, HttpWebRequest webRequest)
        {
            if (request.Method == HttpMethod.Post)
            {
                PopulateWebRequestHeaders(webRequest, GetDefaultPostHeaders(request.Protocol));
            }
            if (request.Method == HttpMethod.Get)
            {
                PopulateWebRequestHeaders(webRequest, GetDefaultHeaders(request.Protocol));
            }
        }

        private static void PopulateWebRequestHeaders(HttpWebRequest webRequest, IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                if( header.Key == "Accept" )
                    webRequest.Accept = header.Value;
                else if( header.Key == "Content-Type" )
                    webRequest.ContentType = header.Value;
                else
                    webRequest.Headers[ header.Key ] = header.Value;
            }
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultHeaders(Protocol protocol)
        {
            if (protocol == Protocol.Json)
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("json"));
            }
            else
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("binary"));
            }
        }

        internal static IEnumerable<KeyValuePair<string, string>> GetDefaultPostHeaders(Protocol protocol)
        {
            if (protocol == Protocol.Json)
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("json"));
                yield return new KeyValuePair<string, string>("Content-Type", MimeTypes.GetHeaderValue("json"));
            }
            else
            {
                yield return new KeyValuePair<string, string>("Accept", MimeTypes.GetHeaderValue("binary"));
                yield return new KeyValuePair<string, string>("Content-Type", MimeTypes.GetHeaderValue("binary"));
            }
        }

        private static AblyResponse GetAblyResponse(HttpWebResponse response)
        {
            return new AblyResponse( response.Headers[ "Content-Encoding" ], response.ContentType, ReadFully( response.GetResponseStream() ) )
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers
            };
        }

        private static byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private Uri GetRequestUrl(AblyRequest request)
        {
            string protocol = _isSecure ? "https://" : "http://";
            if (request.Url.StartsWith("http"))
                return new Uri(request.Url);
            return new Uri(String.Format("{0}{1}{2}{3}{4}",
                               protocol,
                               GetHost(),
                               _port.HasValue ? ":" + _port.Value : "",
                               request.Url,
                               GetQuery(request)));
        }

        private string GetHost()
        {
            if(_isDefaultHost && (_environment.HasValue && _environment != AblyEnvironment.Live))
                return _environment.ToString().ToLower() + "-" + _host;
            return _host;
        }

        private string GetQuery(AblyRequest request)
        {
            string query = string.Join("&", request.QueryParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            if (query.IsNotEmpty())
                return "?" + query;
            return string.Empty;
        }
    }
}
