using System;
using System.Threading.Tasks;

namespace IO.Ably
{
    public interface IAblyAuth
    {
        Task<TokenDetails> RequestTokenAsync(TokenParams tokenParams = null, AuthOptions options = null);
        Task<TokenDetails> AuthorizeAsync(TokenParams tokenParams = null, AuthOptions options = null);

        [Obsolete("This method will be removed in the future, please replace with a call to AuthorizeAsync")]
        Task<TokenDetails> AuthoriseAsync(TokenParams tokenParams = null, AuthOptions options = null);

        // Async because uses server time,
        /// <summary>Returns a signed TokenRequest object that can be used to obtain a token from Ably.</summary>
        /// <remarks>This method is asynchronous, because when <see cref="AuthOptions.QueryTime"/> is set to true, it will issue a request and wait for response.</remarks>
        Task<string> CreateTokenRequestAsync(TokenParams tokenParams = null, AuthOptions authOptions = null);

        string ClientId { get; }

        TokenDetails RequestToken(TokenParams tokenParams = null,
            AuthOptions options = null);

        TokenDetails Authorize(TokenParams tokenParams = null,
            AuthOptions options = null);

        [Obsolete("This method will be removed in the future, please replace with a call to Authorize")]
        TokenDetails Authorise(TokenParams tokenParams = null,
            AuthOptions options = null);

        string CreateTokenRequest(TokenParams tokenParams = null,
            AuthOptions authOptions = null);
    }
}