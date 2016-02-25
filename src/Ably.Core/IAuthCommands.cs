using IO.Ably.Auth;

namespace IO.Ably
{
    public interface IAuthCommands
    {
        TokenDetails RequestToken(TokenRequest request, AuthOptions options);
        TokenDetails Authorise(TokenRequest request, AuthOptions options, bool force);
        TokenRequestPostData CreateTokenRequest(TokenRequest request, AuthOptions options);
    }
}