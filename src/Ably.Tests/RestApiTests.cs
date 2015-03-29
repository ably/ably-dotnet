using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace Ably.Tests
{
    public abstract class RestApiTests
    {
        protected const string ValidKey = "AHSz6w.uQXPNQ:FGBZbsKSwqbCpkob";
        protected AblyRequest _currentRequest;
        internal MimeTypes mimeTypes = new MimeTypes();
        
        protected Rest GetRestClient()
        {
            var rest = new Rest(ValidKey);
        
            rest.ExecuteRequest = x => { _currentRequest = x; return new AblyResponse(); };
            return rest;
        }

    }
    public class TimeTests : RestApiTests
    {
        [Fact]
        public void Time_ShouldSendGetRequestToCorrectPathWithCorrectHeaders()
        {
            var rest = GetRestClient();

            try
            {
                rest.Time();
            }
            catch
            {
                //ignore processing errors and only care about the request
            }
            Assert.Equal("/time", _currentRequest.Url);
            Assert.Equal(HttpMethod.Get, _currentRequest.Method);
        }
    }

    public static class Headers
    {
        public const string Accept = "Accept";
        public const string ContentType = "Content-Type";
    }

    public static class TestHelpers
    {
        public static void AssertContainsHeader(this AblyRequest request, string key, string value)
        {
            Assert.True(request.Headers.ContainsKey(key), 
                String.Format("Header '{0}' doesn't exist in request", key));
            Assert.Equal(value, request.Headers[key]);
        }

        public static void AssertContainsParameter(this AblyRequest request, string key, string value)
        {
            Assert.True(request.QueryParameters.ContainsKey(key),
                String.Format("Header '{0}' doesn't exist in request", key));
            Assert.Equal(value, request.QueryParameters[key]);
        }
    }
}
