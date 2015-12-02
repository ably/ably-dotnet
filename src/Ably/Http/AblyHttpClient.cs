using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ably
{
    internal class AblyHttpClient : IAblyHttpClient
    {
        internal static readonly MimeTypes MimeTypes = new MimeTypes();

        private string _host;
        private readonly int? _port;
        private readonly bool _isSecure;
        private readonly AblyEnvironment? _environment;
        private readonly RestSharp.RestClient _restClient;
        private bool _isDefaultHost;

        public AblyHttpClient(string host) : this(host, null, true) { }

        public AblyHttpClient(string host, int? port = null, bool isSecure = true, AblyEnvironment? environment = AblyEnvironment.Live)
        {
            _isSecure = isSecure;
            _environment = environment;
            _port = port;
            _host = host;

            _isDefaultHost = host == Config.DefaultHost;
            Uri uri = buildUri(host, port, isSecure, environment);
            _restClient = new RestSharp.RestClient(uri)
            {
                Timeout = Config.ConnectTimeout,
                UserAgent = "Ably.net library"
            };
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
                    Logger.Current.Error("Cumulative retry timeout of {0}s was exceeded", Config.CommulativeFailedRequestTimeOutInSeconds);   
                    throw new AblyException(
                        new ErrorInfo(string.Format("Commulative retry timeout of {0}s was exceeded.", 
                            Config.CommulativeFailedRequestTimeOutInSeconds), 500, null));
                }

                RestSharp.Method method = (RestSharp.Method)Enum.Parse(typeof(RestSharp.Method), request.Method, true);
                RestSharp.RestRequest restRequest = new RestSharp.RestRequest(request.Url, method);

                PopulateDefaultHeaders(request, method, restRequest);
                PopulateWebRequestHeaders(restRequest, request.Headers);

                RestSharp.IRestResponse restResponse = _restClient.Execute(restRequest);

                if (restResponse.ResponseStatus != RestSharp.ResponseStatus.Completed)
                {
                    if (IsRetryable(restResponse) && _isDefaultHost)
                    {
                        Logger.Current.Error("Error making a connection to Ably servers. Retrying", restResponse.ErrorException);
                        _host = hosts[currentTry - 1];
                        currentTry++;
                        continue;
                    }

                    if (restResponse.ContentLength != 0)
                        throw AblyException.FromResponse(GetAblyResponse(restResponse));

                    throw new AblyException(new ErrorInfo("Unexpected error. Check the inner exception for details", 500, null), restResponse.ErrorException);
                }

                return GetAblyResponse(restResponse);
            }
            throw new AblyException(new ErrorInfo("Unexpected error while making a request.", 500, null));
        }

        private bool IsRetryable(RestSharp.IRestResponse response)
        {
            return response.ResponseStatus == RestSharp.ResponseStatus.TimedOut;
                //ex.Status == WebExceptionStatus.ReceiveFailure ||
                //ex.Status == WebExceptionStatus.ConnectFailure ||
                //ex.Status == WebExceptionStatus.Timeout ||
                //ex.Status == WebExceptionStatus.KeepAliveFailure;
        }

        //private AblyResponse ExecuteInternal(AblyRequest request)
        //{
        //    RestSharp.RestRequest restRequest = new RestSharp.RestRequest(request.Url, request.Method);

        //    PopulateDefaultHeaders(request, restRequest);
        //    PopulateWebRequestHeaders(restRequest, request.Headers);

        //    RestSharp.IRestResponse restResponse = _restClient.Execute(restRequest);

        //    if (restResponse.ResponseStatus != RestSharp.ResponseStatus.Completed)
        //    {
        //        var errorResponse = exception.Response as HttpWebResponse;

        //        if (IsRetryable(restResponse) && _isDefaultHost)
        //        {
        //            Logger.Current.Error("Error making a connection to Ably servers. Retrying", restResponse.ErrorException);
        //            _host = hosts[currentTry - 1];
        //            currentTry++;
        //            continue;
        //        }

        //        if (errorResponse != null)
        //            throw AblyException.FromResponse(GetAblyResponse(errorResponse));

        //        throw new AblyException(new ErrorInfo("Unexpected error. Check the inner exception for details", 500, null), exception);
        //    }

        //    return GetAblyResponse(restResponse);
        //}

        private void PopulateDefaultHeaders(AblyRequest request, RestSharp.Method requestMethod, RestSharp.IRestRequest webRequest)
        {
            if (requestMethod == RestSharp.Method.POST)
            {
                PopulateWebRequestHeaders(webRequest, GetDefaultPostHeaders(request.Protocol));
            }
            if (requestMethod == RestSharp.Method.GET)
            {
                PopulateWebRequestHeaders(webRequest, GetDefaultHeaders(request.Protocol));
            }
        }

        private static void PopulateWebRequestHeaders(RestSharp.IRestRequest webRequest, IEnumerable<KeyValuePair<string, string>> headers)
        {
            foreach (var header in headers)
            {
                webRequest.AddHeader(header.Key, header.Value);
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

        private static Uri buildUri(string host, int? port, bool isSecure, AblyEnvironment? environment)
        {
            string actualHost = host;
            if (host == Config.DefaultHost && (environment.HasValue && environment != AblyEnvironment.Live))
                actualHost = string.Format("{0}-{1}", environment.ToString().ToLower(), host);
            UriBuilder builder = new UriBuilder(isSecure ? "https://" : "http://", actualHost);
            if (port.HasValue)
                builder.Port = port.Value;
            return builder.Uri;
        }

        private static AblyResponse GetAblyResponse(RestSharp.IRestResponse response)
        {
            return new AblyResponse(response.ContentEncoding, response.ContentType, response.RawBytes)
            {
                StatusCode = response.StatusCode,
                Headers = Utils.CollectionUtility.ToNameValueCollection(response.Headers)
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

        //private Uri GetRequestUrl(AblyRequest request)
        //{
        //    string protocol = _isSecure ? "https://" : "http://";
        //    if (request.Url.StartsWith("http"))
        //        return new Uri(request.Url);
        //    return new Uri(String.Format("{0}{1}{2}{3}{4}",
        //                       protocol,
        //                       GetHost(),
        //                       _port.HasValue ? ":" + _port.Value : "",
        //                       request.Url,
        //                       GetQuery(request)));
        //}

        //private string GetHost()
        //{
        //    if (_isDefaultHost && (_environment.HasValue && _environment != AblyEnvironment.Live))
        //        return _environment.ToString().ToLower() + "-" + _host;
        //    return _host;
        //}

        private string GetQuery(AblyRequest request)
        {
            string query = string.Join("&", request.QueryParameters.Select(x => String.Format("{0}={1}", x.Key, x.Value)));
            if (query.IsNotEmpty())
                return "?" + query;
            return string.Empty;
        }
    }
}
