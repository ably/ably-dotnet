using Ably.Tests;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.Serialization;
using Xunit.Extensions;
using System.Net.Http;

namespace Ably.Tests
{
    public class RestTests
    {
        private const string ValidKey = "AHSz6w:uQXPNQ:FGBZbsKSwqbCpkob";
        private readonly ApiKey Key = ApiKey.Parse(ValidKey);

        private class RestThatReadsDummyConnectionString : Rest
        {
            internal override string GetConnectionString()
            {
                return "";
            }
        }

        private static Rest GetRestClient()
        {
            return new Rest(ValidKey);
        }

        [Fact]
        public void Ctor_WithNoParametersAndNoAblyConnectionString_Throws()
        {
            var ex = Assert.Throws<AblyException>(delegate {
             new RestThatReadsDummyConnectionString();
            });

            Assert.IsType<ConfigurationMissingException>(ex.InnerException);
        }

        [Fact]
        public void Ctor_WithNoParametersAndAblyConnectionString_RetrievesApiKeyFromConnectionString()
        {
            var rest = new Rest();

            Assert.NotNull(rest);
        }

        [Fact]
        public void Ctor_WithNoParametersWithInvalidKey_ThrowsInvalidKeyException()
        {
            AblyException ex = Assert.Throws<AblyException>(delegate
            {
                new Rest("InvalidKey");
            });

            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        }

        [Fact]
        public void Ctor_WithKeyPassedInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithKeyInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.Key = ValidKey);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithAppIdInOptions_InitialisesClient()
        {
            var client = new Rest(opts => opts.AppId = Key.AppId);
            Assert.NotNull(client);
        }

        [Fact]
        public void Init_WithNoAppIdOrKey_Throws()
        {
            var ex = Assert.Throws<AblyException>(delegate { new Rest(""); });

            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        [Fact]
        public void ChannelsGet_ReturnsNewChannelWithName()
        {
            var rest = GetRestClient();

            var channel = rest.Channels.Get("Test");

            Assert.Equal("Test", channel.Name);
        }

        [Fact]
        public void Stats_CreatesGetRequestWithCorrectPath()
        {
            var rest = GetRestClient();
            

            AblyRequest request = null;
            rest.ExecuteRequest = x => { request = x; return (AblyResponse)null; };
            rest.Stats();

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/apps/" + rest.Options.AppId + "/stats", request.Path);
        }

        
        [Fact]
        public void Stats_WithQuery_SetsCorrectRequestHeaders()
        {
            var rest = GetRestClient();
            AblyRequest request = null;
            rest.ExecuteRequest = x => { request = x; return (AblyResponse)null; };
            var query = new DataRequestQuery();
            DateTime now = DateTime.Now;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            rest.Stats(query);

            request.AssertContainsParameter("start", query.Start.Value.ToUnixTime().ToString());
            request.AssertContainsParameter("end", query.End.Value.ToUnixTime().ToString());
            request.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            request.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }

        [Fact]
        public void History_WithNoOptions_CreateGetRequestWithValidPath()
        {
            var rest = GetRestClient();
            AblyRequest request = null;
            rest.ExecuteRequest = x => { request = x; return (AblyResponse)null; };
            rest.History();

            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(String.Format("/apps/{0}/history", rest.Options.AppId), request.Path);
        }

        [Fact]
        public void History_WithOptions_AddsParametersToRequest()
        {
            var rest = GetRestClient();
            AblyRequest request = null;
            rest.ExecuteRequest = x => { request = x; return (AblyResponse)null; };

            var query = new DataRequestQuery();
            DateTime now = DateTime.Now;
            query.Start = now.AddHours(-1);
            query.End = now;
            query.Direction = QueryDirection.Forwards;
            query.Limit = 1000;
            rest.History(query);

            request.AssertContainsParameter("start", query.Start.Value.ToUnixTime().ToString());
            request.AssertContainsParameter("end", query.End.Value.ToUnixTime().ToString());
            request.AssertContainsParameter("direction", query.Direction.ToString().ToLower());
            request.AssertContainsParameter("limit", query.Limit.Value.ToString());
        }
    }
}
