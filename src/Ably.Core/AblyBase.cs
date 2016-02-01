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

        /// <summary>
        /// Initializes the Ably Auth type based on the options passed.
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

        private void LogCurrentAuthenticationMethod()
        {
            if (Options.AuthCallback != null)
            {
                Logger.Info("Authentication will be done using token auth with authCallback");
            }
            else if (Options.AuthUrl.IsNotEmpty())
            {
                Logger.Info( "Authentication will be done using token auth with url {0}", Options.AuthUrl );
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
