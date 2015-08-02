using Ably.Auth;
using Ably.Rest;

namespace Ably
{
    public class AblyBase
    {
        protected readonly ILogger Logger = Config.AblyLogger;
        protected Protocol _protocol;
        protected AblyOptions _options;
        internal AuthMethod AuthMethod;
        internal TokenDetails CurrentToken;
        private IAuthCommands _auth;

        /// <summary>
        /// Authentication methods
        /// </summary>
        public IAuthCommands Auth
        {
            get { return _auth; }
        }

        internal Protocol Protocol
        {
            get { return _protocol; }
        }

        internal AblyOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Initialises the Ably Auth type based on the options passed.
        /// </summary>
        internal void InitAuth(IAblyRest restClient)
        {
            _auth = new AblyTokenAuth(Options, restClient);
            AuthMethod = Options.Method;
            if (AuthMethod == AuthMethod.Basic)
            {
                Logger.Info("Using basic authentication.");
                return;
            }
            Logger.Info("Using token authentication.");
            if (Options.Token.IsNotEmpty())
            {
                CurrentToken = new TokenDetails(Options.Token);
            }
            LogCurrentAuthenticationMethod();
        }

        private void LogCurrentAuthenticationMethod()
        {
            if (Options.AuthCallback != null)
            {
                Logger.Info("Authentication will be done using token auth with authCallback");
            }
            else if (Options.AuthUrl.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with authUrl");
            }
            else if (Options.Key.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with client-side signing");
            }
            else if (Options.Token.IsNotEmpty())
            {
                Logger.Info("Authentication will be done using token auth with supplied token only");
            }
            else
            {
                /* this is not a hard error - but any operation that requires
                 * authentication will fail */
                Logger.Info("Authentication will fail because no authentication parameters supplied");
            }
        }
    }
}
