using IO.Ably.Auth;
using System.Threading.Tasks;

namespace IO.Ably
{
    public interface IAuthCommands
    {
        Task<TokenDetails> RequestTokenAsync(TokenParams tokenParams = null, AuthOptions options = null);
        Task<TokenDetails> AuthoriseAsync(TokenParams tokenParams = null, AuthOptions options = null);

        // Async because uses server time,
        /// <summary>Returns a signed TokenRequest object that can be used to obtain a token from Ably.</summary>
        /// <remarks>This method is asynchronous, because when <see cref="AuthOptions.QueryTime"/> is set to true, it will issue a request and wait for response.</remarks>
        Task<TokenRequest> CreateTokenRequestAsync(TokenParams tokenParams = null, AuthOptions options = null);

        AuthMethod AuthMethod { get; }

        Task<TokenDetails> GetCurrentValidTokenAndRenewIfNecessaryAsync();

        void ExpireCurrentToken();
    }
}