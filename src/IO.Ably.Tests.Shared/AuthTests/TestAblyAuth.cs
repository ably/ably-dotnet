using System;
using System.Threading.Tasks;

namespace IO.Ably.Tests.AuthTests
{
    internal class TestAblyAuth : AblyAuth
    {
        public bool RequestTokenCalled { get; set; }

        public TokenParams LastRequestTokenParams { get; set; }

        public AuthOptions LastRequestAuthOptions { get; set; }

        public override Task<TokenDetails> RequestTokenAsync(TokenParams tokenParams, AuthOptions options)
        {
            RequestTokenCalled = true;
            LastRequestTokenParams = tokenParams;
            LastRequestAuthOptions = options;

            return base.RequestTokenAsync(tokenParams, options);
        }

        public TestAblyAuth(ClientOptions options, AblyRest rest, Func<Task<DateTimeOffset>> serverTimeFunc = null)
            : base(options, rest)
        {
            if (serverTimeFunc != null)
            {
                ServerTime = serverTimeFunc;
            }
        }

        // Exposes the protected property ServerTime
        public async Task<DateTimeOffset> GetServerTime()
        {
            return await ServerTime();
        }

        // Exposes the protected property ServerTimeOffset
        public DateTimeOffset? GetServerNow()
        {
            return ServerNow();
        }
    }
}
