using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using Xunit;

namespace Ably.Tests
{
    public class StatsParsingTests
    {
        Stats _stats; 

        public StatsParsingTests()
        {
           _stats = JsonConvert.DeserializeObject<List<Stats>>(File.ReadAllText(@"Json\StatsInterval.json")).First(); 
        }        

        [Fact]
        public void AllSectionHasCorrectValues()
        {
            Assert.Equal(20, _stats.All.All.Count);
            Assert.Equal(180, _stats.All.All.Data);
            Assert.Equal(20, _stats.All.Messages.Count);
            Assert.Equal(180, _stats.All.Messages.Data);
            Assert.Equal(0, _stats.All.Presence.Count);
            Assert.Equal(0, _stats.All.Presence.Data);
        }

        [Fact]
        public void InboundSectionHasCorrectValues()
        {
            Assert.Equal(20, _stats.Inbound.All.All.Count);
            Assert.Equal(180, _stats.Inbound.All.All.Data);
            Assert.Equal(20, _stats.Inbound.All.Messages.Count);
            Assert.Equal(180, _stats.Inbound.All.Messages.Data);
            Assert.Equal(0, _stats.Inbound.All.Presence.Count);
            Assert.Equal(0, _stats.Inbound.All.Presence.Data);
        
            //Realtime
            Assert.Equal(0, _stats.Inbound.Realtime.All.Count);
            Assert.Equal(0, _stats.Inbound.Realtime.All.Data);
            Assert.Equal(0, _stats.Inbound.Realtime.Messages.Count);
            Assert.Equal(0, _stats.Inbound.Realtime.Messages.Data);
            Assert.Equal(0, _stats.Inbound.Realtime.Presence.Count);
            Assert.Equal(0, _stats.Inbound.Realtime.Presence.Data);

            //Rest
            Assert.Equal(20, _stats.Inbound.Rest.All.Count);
            Assert.Equal(180, _stats.Inbound.Rest.All.Data);
            Assert.Equal(20, _stats.Inbound.Rest.Messages.Count);
            Assert.Equal(180, _stats.Inbound.Rest.Messages.Data);
            Assert.Equal(0, _stats.Inbound.Rest.Presence.Count);
            Assert.Equal(0, _stats.Inbound.Rest.Presence.Data);
        
            //Push
            Assert.Equal(0, _stats.Inbound.Push.All.Count);
            Assert.Equal(0, _stats.Inbound.Push.All.Data);
            Assert.Equal(0, _stats.Inbound.Push.Messages.Count);
            Assert.Equal(0, _stats.Inbound.Push.Messages.Data);
            Assert.Equal(0, _stats.Inbound.Push.Presence.Count);
            Assert.Equal(0, _stats.Inbound.Push.Presence.Data);
        
            //HttpStream
            Assert.Equal(0, _stats.Inbound.HttpStream.All.Count);
            Assert.Equal(0, _stats.Inbound.HttpStream.All.Data);
            Assert.Equal(0, _stats.Inbound.HttpStream.Messages.Count);
            Assert.Equal(0, _stats.Inbound.HttpStream.Messages.Data);
            Assert.Equal(0, _stats.Inbound.HttpStream.Presence.Count);
            Assert.Equal(0, _stats.Inbound.HttpStream.Presence.Data);
        }

        [Fact]
        public void OutboundSectionHasCorrectValues()
        {
            Assert.Equal(0, _stats.Outbound.All.All.Count);
            Assert.Equal(0, _stats.Outbound.All.All.Data);
            Assert.Equal(0, _stats.Outbound.All.Messages.Count);
            Assert.Equal(0, _stats.Outbound.All.Messages.Data);
            Assert.Equal(0, _stats.Outbound.All.Presence.Count);
            Assert.Equal(0, _stats.Outbound.All.Presence.Data);

            //Realtime
            Assert.Equal(0, _stats.Outbound.Realtime.All.Count);
            Assert.Equal(0, _stats.Outbound.Realtime.All.Data);
            Assert.Equal(0, _stats.Outbound.Realtime.Messages.Count);
            Assert.Equal(0, _stats.Outbound.Realtime.Messages.Data);
            Assert.Equal(0, _stats.Outbound.Realtime.Presence.Count);
            Assert.Equal(0, _stats.Outbound.Realtime.Presence.Data);
                                    
            //Rest                  
            Assert.Equal(0, _stats.Outbound.Rest.All.Count);
            Assert.Equal(0, _stats.Outbound.Rest.All.Data);
            Assert.Equal(0, _stats.Outbound.Rest.Messages.Count);
            Assert.Equal(0, _stats.Outbound.Rest.Messages.Data);
            Assert.Equal(0, _stats.Outbound.Rest.Presence.Count);
            Assert.Equal(0, _stats.Outbound.Rest.Presence.Data);
                                    
            //Post                  
            Assert.Equal(0, _stats.Outbound.Push.All.Count);
            Assert.Equal(0, _stats.Outbound.Push.All.Data);
            Assert.Equal(0, _stats.Outbound.Push.Messages.Count);
            Assert.Equal(0, _stats.Outbound.Push.Messages.Data);
            Assert.Equal(0, _stats.Outbound.Push.Presence.Count);
            Assert.Equal(0, _stats.Outbound.Push.Presence.Data);
                                    
            //HttpStream            
            Assert.Equal(0, _stats.Outbound.HttpStream.All.Count);
            Assert.Equal(0, _stats.Outbound.HttpStream.All.Data);
            Assert.Equal(0, _stats.Outbound.HttpStream.Messages.Count);
            Assert.Equal(0, _stats.Outbound.HttpStream.Messages.Data);
            Assert.Equal(0, _stats.Outbound.HttpStream.Presence.Count);
            Assert.Equal(0, _stats.Outbound.HttpStream.Presence.Data);
        }

        [Fact]
        public void PersistedHasCorrectValues()
        {
            Assert.Equal(0, _stats.Persisted.All.Count);
            Assert.Equal(0, _stats.Persisted.All.Data);
            Assert.Equal(0, _stats.Persisted.Messages.Count);
            Assert.Equal(0, _stats.Persisted.Messages.Data);
            Assert.Equal(0, _stats.Persisted.Presence.Count);
            Assert.Equal(0, _stats.Persisted.Presence.Data);
        }

        [Fact]
        public void ConnectionsHasCorrectValues()
        {
            Assert.Equal(0, _stats.Connections.All.Opened);
            Assert.Equal(0, _stats.Connections.All.Peak);
            Assert.Equal(0, _stats.Connections.All.Mean);
            Assert.Equal(0, _stats.Connections.All.Min);
            Assert.Equal(0, _stats.Connections.All.Refused);

            Assert.Equal(0, _stats.Connections.Plain.Opened);
            Assert.Equal(0, _stats.Connections.Plain.Peak);
            Assert.Equal(0, _stats.Connections.Plain.Mean);
            Assert.Equal(0, _stats.Connections.Plain.Min);
            Assert.Equal(0, _stats.Connections.Plain.Refused);

            Assert.Equal(0, _stats.Connections.Tls.Opened);
            Assert.Equal(0, _stats.Connections.Tls.Peak);
            Assert.Equal(0, _stats.Connections.Tls.Mean);
            Assert.Equal(0, _stats.Connections.Tls.Min);
            Assert.Equal(0, _stats.Connections.Tls.Refused);
        }

        [Fact]
        public void ChannelsHasCorrectValues()
        {
            Assert.Equal(1, _stats.Channels.Opened);
            Assert.Equal(0, _stats.Channels.Peak);
            Assert.Equal(0, _stats.Channels.Mean);
            Assert.Equal(0, _stats.Channels.Min);
            Assert.Equal(0, _stats.Channels.Refused);
        }

        [Fact]
        public void ApiRequestsHasCorrectValues()
        {
            Assert.Equal(20, _stats.ApiRequests.Succeeded);
            Assert.Equal(0, _stats.ApiRequests.Failed);
            Assert.Equal(0, _stats.ApiRequests.Refused);
        }

        [Fact]
        public void TokenRequestsHasCorrectValues()
        {
            Assert.Equal(0, _stats.TokenRequests.Succeeded);
            Assert.Equal(0, _stats.TokenRequests.Failed);
            Assert.Equal(0, _stats.TokenRequests.Refused);
        }

        [Fact]
        public void IntervalIDHasCorrectValue()
        {
            Assert.Equal(new DateTime(2014, 01, 01, 16, 24, 00), _stats.Interval);
        }
    }
}
