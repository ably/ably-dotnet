using Ably.Auth;
using Ably.Rest;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Ably
{
    public class AblyBase
    {
        private static readonly string InternetCheckURL = "https://internet-up.ably-realtime.com/is-the-internet-up.txt";
        private static readonly string InternetCheckOK = "yes";

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

        /// <summary>Initialises the Ably Auth type based on the options passed.</summary>
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

        public static bool CanConnectToAbly()
        {
            WebRequest req = WebRequest.Create(InternetCheckURL);
            WebResponse res = null;
            try
            {
                Func<Task<WebResponse>> fn = () => req.GetResponseAsync();
                res = Task.Run( fn ).Result;
            }
            catch (Exception)
            {
                return false;
            }
            using( var resStream = res.GetResponseStream() )
            {
                using( StreamReader reader = new StreamReader( resStream ) )
                {
                    return reader.ReadLine() == InternetCheckOK;
                }
            }
        }

        internal TokenAuthMethod GetTokenAuthMethod()
        {
            if( null != Options.AuthCallback )
                return TokenAuthMethod.Callback;
            if( Options.AuthUrl.IsNotEmpty() )
                return TokenAuthMethod.Url;
            if( Options.Key.IsNotEmpty() )
                return TokenAuthMethod.Signing;
            if( Options.Token.IsNotEmpty() )
                return TokenAuthMethod.JustToken;
            return TokenAuthMethod.None;
        }

        private void LogCurrentAuthenticationMethod()
        {
            TokenAuthMethod method = GetTokenAuthMethod();
            Logger.Info( "Authentication method: {0}", method.description() );
        }

        /// <summary>True if CurrentToken is still valid.</summary>
        protected bool HasValidToken()
        {
            if ( null == CurrentToken )
                return false;
            DateTimeOffset exp = CurrentToken.Expires;
            return ( exp == DateTimeOffset.MinValue ) || ( exp >= DateTimeOffset.UtcNow );
        }
    }
}