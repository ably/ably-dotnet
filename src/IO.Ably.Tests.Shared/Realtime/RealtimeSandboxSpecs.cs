using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Events;
using IO.Ably.Encryption;
using IO.Ably.Realtime;
using IO.Ably.Rest;
using IO.Ably.Tests.Infrastructure;
using IO.Ably.Transport.States.Connection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IO.Ably.Tests.Realtime
{
    [Collection("Realtime SandBox")]
    [Trait("requires", "sandbox")]
    public class RealtimeSandboxSpecs : SandboxSpecs
    {
        public RealtimeSandboxSpecs(AblySandboxFixture fixture, ITestOutputHelper output) : base(fixture, output){}


    }
}
