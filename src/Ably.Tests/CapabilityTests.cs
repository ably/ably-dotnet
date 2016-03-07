using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Ably.Tests
{
    public class CapabilityTests
    {
        [Fact]
        public void Capability_WithOutAnyResources_ReturnsJsonString()
        {
            var capability = new Capability();
            Assert.Equal("", capability.ToJson());
        }

        [Fact]
        public void Capability_WithResource_ReturnsCorrectJsonString()
        {
            var capability = new Capability();

            capability
                .AddResource("name").AllowPublish() ;

            Assert.Equal("{ \"name\": [ \"publish\" ] }", capability.ToJson());
            
        }

        [Fact]
        public void Capability_WithResourceThatHasPublishSubscribeAndPresence_ReturnsJsonStringWithCorrectResourceOrder()
        {
            var capability = new Capability();

            capability
                .AddResource("name").AllowSubscribe().AllowPublish().AllowPresence();

            Assert.Equal("{ \"name\": [ \"presence\", \"publish\", \"subscribe\" ] }", capability.ToJson());
        }

        [Fact]
        public void Capability_WithResourceThatHasPublishAndThenAll_ReturnsJsonWithResourceEqualToStarWithoutShowingPublish()
        {
            var capability = new Capability();
            capability.AddResource("name").AllowPublish().AllowAll();

            Assert.Equal("{ \"name\": [ \"*\" ] }", capability.ToJson());
        }

        [Fact]
        public void Capability_WithResourceThatHasNoAllowedOperations_DoesNotIncludeResourceInJson()
        {
            var capability = new Capability();
            capability.AddResource("name");

            Assert.Equal("", capability.ToJson());
        }

        [Fact]
        public void Capability_WithMultipleResources_OrdersThemInAlphabeticalOrder()
        {
            var capability = new Capability();

            capability.AddResource("second").AllowPublish();
            capability.AddResource("first").AllowAll();

            Assert.Equal("{ \"first\": [ \"*\" ], \"second\": [ \"publish\" ] }", capability.ToJson());
        }

        [Fact]
        public void Capability_InitializedWith2Resources_AddsThemCorrectlyToAllowedResourced()
        {
            var capabilityString = "{ \"first\": [ \"*\" ], \"second\": [ \"publish\" ] }";
            var capability = new Capability(capabilityString);

            Assert.Equal(2, capability.Resources.Count);    
            Assert.Equal("first", capability.Resources.First().Name);
            Assert.Equal("*", capability.Resources.First().AllowedOperations.First());
        }

        [Fact]
        public void Capability_WithDefaultAllAll_AddsThemCorrectly()
        {
            var capabilityString = "{ \"*\": [ \"*\" ] }";
            var capability = new Capability(capabilityString);

            capability.ToJson().Should().Be(capabilityString);
        }
        
    }
}
