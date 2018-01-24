using System;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    public class FakeAuth : IAblyAuth
    {
        public TokenDetails CurrentToken { get; set; }

        public Task<TokenDetails> RequestTokenAsync(TokenParams tokenParams = null, AuthOptions options = null)
        {
            return Task.FromResult(CurrentToken);
        }

        public Task<TokenDetails> AuthoriseAsync(TokenParams tokenParams = null, AuthOptions options = null)
        {
            return Task.FromResult(CurrentToken);
        }

        // Async because uses server time,
        /// <summary>Returns a signed TokenRequest object that can be used to obtain a token from Ably.</summary>
        /// <remarks>This method is asynchronous, because when <see cref="AuthOptions.QueryTime"/> is set to true, it will issue a request and wait for response.</remarks>
        public Task<string> CreateTokenRequestAsync(TokenParams tokenParams = null, AuthOptions authOptions = null)
        {
            throw new NotImplementedException();
        }

        public AuthMethod AuthMethod { get; set; }
        public string ClientId { get; set; }

        public TokenDetails RequestToken(TokenParams tokenParams = null, AuthOptions options = null)
        {
            return CurrentToken;
        }

        public TokenDetails Authorise(TokenParams tokenParams = null, AuthOptions options = null)
        {
            return CurrentToken;
        }

        public string CreateTokenRequest(TokenParams tokenParams = null, AuthOptions authOptions = null)
        {
            throw new NotImplementedException();
        }

        public Task<TokenDetails> GetCurrentValidTokenAndRenewIfNecessaryAsync()
        {
            return Task.FromResult(CurrentToken);
        }

        public void ExpireCurrentToken()
        {
            CurrentToken?.Expire();
        }
    }
}