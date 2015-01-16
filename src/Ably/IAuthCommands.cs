using Ably.Auth;

namespace Ably
{
    public interface IAuthCommands
    {
        Token RequestToken(TokenRequest request, AuthOptions options);
        Token Authorise(TokenRequest request, AuthOptions options, bool force);
        TokenRequestPostData CreateTokenRequest(TokenRequest request, AuthOptions options);
    }
}