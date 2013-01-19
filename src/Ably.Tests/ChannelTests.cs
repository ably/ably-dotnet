using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace Ably.Tests
{
    public class ChannelTests : RestApiTests
    {
        [Fact]
        public void Publish_CreatesPostRequestWithValidPath()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", "data");

            Assert.Equal(HttpMethod.Post, _currentRequest.Method);
            Assert.Equal("/apps/" + rest.Options.AppId + "/channels/" + channel.Name + "/publish", _currentRequest.Path);
        }

        [Fact]
        public void Publish_WithNameAndData_AddsPayloadToRequest()
        {
            var rest = GetRestClient();
            var channel = rest.Channels.Get("Test");
            channel.Publish("event", "data");

            Assert.IsType<ChannelPublishPayload>(_currentRequest.Data);
            var data = _currentRequest.Data as ChannelPublishPayload;
            Assert.Equal("data", data.Data);
            Assert.Equal("event", data.Name);
        }
    }

    
}
