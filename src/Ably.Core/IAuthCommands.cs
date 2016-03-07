using IO.Ably.Auth;
using System.Threading.Tasks;

namespace IO.Ably
{
    public interface IAuthCommands
    {
        Task<TokenDetails> RequestToken(TokenRequest request, AuthOptions options);
        Task<TokenDetails> Authorise(TokenRequest request, AuthOptions options, bool force);

        // Async because uses server time,

        /// <summary>Returns a signed TokenRequest object that can be used to obtain a token from Ably.</summary>
        /// <remarks>This method is asynchronous, because when <see cref="AuthOptions.QueryTime"/> is set to true, it will issue a request and wait for response.</remarks>
        Task<TokenRequestPostData> CreateTokenRequest(TokenRequest request, AuthOptions options);
    }
}