using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ably.Tests
{
    public class StatsParsingTests
    {
        Stats _stats; 

        public StatsParsingTests()
        {
           _stats = JsonConvert.DeserializeObject<Stats>(File.ReadAllText(@"Json\StatsInterval.json")); 
        }        

        [Fact]
        public void AllSectionHasCorrectValues()
        {
            Assert.Equal(10, _stats.All.All.Count);
            Assert.Equal(19.3, _stats.All.All.Data);
            Assert.Equal(10, _stats.All.Messages.Count);
            Assert.Equal(11.3, _stats.All.Messages.Data);
            Assert.Equal(10, _stats.All.Presence.Count);
            Assert.Equal(33.4, _stats.All.Presence.Data);
        }

        [Fact]
        public void InboundSectionHasCorrectValues()
        {
            Assert.Equal(11, _stats.Inbound.All.All.Count);
            Assert.Equal(12, _stats.Inbound.All.All.Data);
            Assert.Equal(13, _stats.Inbound.All.Messages.Count);
            Assert.Equal(14, _stats.Inbound.All.Messages.Data);
            Assert.Equal(15, _stats.Inbound.All.Presence.Count);
            Assert.Equal(16, _stats.Inbound.All.Presence.Data);
        
            //Realtime
            Assert.Equal(17, _stats.Inbound.Realtime.All.Count);
            Assert.Equal(18, _stats.Inbound.Realtime.All.Data);
            Assert.Equal(19, _stats.Inbound.Realtime.Messages.Count);
            Assert.Equal(20, _stats.Inbound.Realtime.Messages.Data);
            Assert.Equal(21, _stats.Inbound.Realtime.Presence.Count);
            Assert.Equal(22, _stats.Inbound.Realtime.Presence.Data);

            //Rest
            Assert.Equal(23, _stats.Inbound.Rest.All.Count);
            Assert.Equal(24, _stats.Inbound.Rest.All.Data);
            Assert.Equal(25, _stats.Inbound.Rest.Messages.Count);
            Assert.Equal(26, _stats.Inbound.Rest.Messages.Data);
            Assert.Equal(27, _stats.Inbound.Rest.Presence.Count);
            Assert.Equal(28, _stats.Inbound.Rest.Presence.Data);
        
            //Post
            Assert.Equal(29, _stats.Inbound.Post.All.Count);
            Assert.Equal(30, _stats.Inbound.Post.All.Data);
            Assert.Equal(31, _stats.Inbound.Post.Messages.Count);
            Assert.Equal(32, _stats.Inbound.Post.Messages.Data);
            Assert.Equal(33, _stats.Inbound.Post.Presence.Count);
            Assert.Equal(34, _stats.Inbound.Post.Presence.Data);
        
            //HttpStream
            Assert.Equal(35, _stats.Inbound.HttpStream.All.Count);
            Assert.Equal(36, _stats.Inbound.HttpStream.All.Data);
            Assert.Equal(37, _stats.Inbound.HttpStream.Messages.Count);
            Assert.Equal(38, _stats.Inbound.HttpStream.Messages.Data);
            Assert.Equal(39, _stats.Inbound.HttpStream.Presence.Count);
            Assert.Equal(40, _stats.Inbound.HttpStream.Presence.Data);
        }

        [Fact]
        public void OutboundSectionHasCorrectValues()
        {
            Assert.Equal(41, _stats.Outbound.All.All.Count);
            Assert.Equal(42, _stats.Outbound.All.All.Data);
            Assert.Equal(43, _stats.Outbound.All.Messages.Count);
            Assert.Equal(44, _stats.Outbound.All.Messages.Data);
            Assert.Equal(45, _stats.Outbound.All.Presence.Count);
            Assert.Equal(46, _stats.Outbound.All.Presence.Data);

            //Realtime
            Assert.Equal(47, _stats.Outbound.Realtime.All.Count);
            Assert.Equal(48, _stats.Outbound.Realtime.All.Data);
            Assert.Equal(49, _stats.Outbound.Realtime.Messages.Count);
            Assert.Equal(50, _stats.Outbound.Realtime.Messages.Data);
            Assert.Equal(51, _stats.Outbound.Realtime.Presence.Count);
            Assert.Equal(52, _stats.Outbound.Realtime.Presence.Data);
                                    
            //Rest                  
            Assert.Equal(53, _stats.Outbound.Rest.All.Count);
            Assert.Equal(54, _stats.Outbound.Rest.All.Data);
            Assert.Equal(55, _stats.Outbound.Rest.Messages.Count);
            Assert.Equal(56, _stats.Outbound.Rest.Messages.Data);
            Assert.Equal(57, _stats.Outbound.Rest.Presence.Count);
            Assert.Equal(58, _stats.Outbound.Rest.Presence.Data);
                                    
            //Post                  
            Assert.Equal(59, _stats.Outbound.Post.All.Count);
            Assert.Equal(60, _stats.Outbound.Post.All.Data);
            Assert.Equal(61, _stats.Outbound.Post.Messages.Count);
            Assert.Equal(62, _stats.Outbound.Post.Messages.Data);
            Assert.Equal(63, _stats.Outbound.Post.Presence.Count);
            Assert.Equal(64, _stats.Outbound.Post.Presence.Data);
                                    
            //HttpStream            
            Assert.Equal(65, _stats.Outbound.HttpStream.All.Count);
            Assert.Equal(66, _stats.Outbound.HttpStream.All.Data);
            Assert.Equal(67, _stats.Outbound.HttpStream.Messages.Count);
            Assert.Equal(68, _stats.Outbound.HttpStream.Messages.Data);
            Assert.Equal(69, _stats.Outbound.HttpStream.Presence.Count);
            Assert.Equal(70, _stats.Outbound.HttpStream.Presence.Data);
        }

        [Fact]
        public void PersistedHasCorrectValues()
        {
            Assert.Equal(20, _stats.Persisted.All.Count);
            Assert.Equal(180, _stats.Persisted.All.Data);
            Assert.Equal(20, _stats.Persisted.Messages.Count);
            Assert.Equal(180, _stats.Persisted.Messages.Data);
            Assert.Equal(0, _stats.Persisted.Presence.Count);
            Assert.Equal(0, _stats.Persisted.Presence.Data);
        }

        [Fact]
        public void ConnectionsHasCorrectValues()
        {
            Assert.Equal(0, _stats.Connections.All.Opened);
            Assert.Equal(0, _stats.Connections.All.Peak);
            Assert.Equal(null, _stats.Connections.All.Mean);
            Assert.Equal(0, _stats.Connections.All.Min);
            Assert.Equal(0, _stats.Connections.All.Refused);

            Assert.Equal(null, _stats.Connections.Plain.Opened);
            Assert.Equal(null, _stats.Connections.Plain.Peak);
            Assert.Equal(0, _stats.Connections.Plain.Mean);
            Assert.Equal(null, _stats.Connections.Plain.Min);
            Assert.Equal(null, _stats.Connections.Plain.Refused);

            Assert.Equal(null, _stats.Connections.Tls.Opened);
            Assert.Equal(null, _stats.Connections.Tls.Peak);
            Assert.Equal(0, _stats.Connections.Tls.Mean);
            Assert.Equal(null, _stats.Connections.Tls.Min);
            Assert.Equal(null, _stats.Connections.Tls.Refused);
        }

        [Fact]
        public void ChannelsHasCorrectValues()
        {
            Assert.Equal(1, _stats.Channels.Opened);
            Assert.Equal(1, _stats.Channels.Peak);
            Assert.Equal(1, _stats.Channels.Mean);
            Assert.Equal(null, _stats.Channels.Min);
            Assert.Equal(null, _stats.Channels.Refused);
        }

        [Fact]
        public void ApiRequestsHasCorrectValues()
        {
            Assert.Equal(1, _stats.ApiRequests.Succeeded);
            Assert.Equal(null, _stats.ApiRequests.Failed);
            Assert.Equal(null, _stats.ApiRequests.Refused);
        }

        [Fact]
        public void TokenRequestsHasCorrectValues()
        {
            Assert.Equal(null, _stats.TokenRequests.Succeeded);
            Assert.Equal(1, _stats.TokenRequests.Failed);
            Assert.Equal(null, _stats.TokenRequests.Refused);
        }

        [Fact]
        public void IntervalIDHasCorrectValue()
        {
            Assert.Equal(new DateTime(2013, 02, 07, 22, 31, 00), _stats.Interval);
        }
    }
}
