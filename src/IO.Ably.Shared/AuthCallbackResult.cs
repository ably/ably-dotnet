using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Ably.Shared
{
    /// <summary>
    /// Returned from an AuthCallback.
    /// Where possible exceptions should be handled in the Authcallback and an ErrorInfo set.
    /// A null ErrorInfo indicates to Ably there were no errors.
    /// </summary>
    public class AuthCallbackResult
    {
        /// <summary>
        /// Get and set a signed <see cref="TokenRequest"/>, <see cref="TokenDetails"/> or a token string.
        /// </summary>
        public object TokenCompatibleObject { get; set; }

        /// <summary>
        /// Get and set <see cref="ErrorInfo"/>. Settings a non-null value indicates to Ably that an error has occured.
        /// </summary>
        public ErrorInfo ErrorInfo { get; set; }

        public AuthCallbackResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthCallbackResult"/> class
        /// </summary>
        /// <param name="tokenCompatibleObject">a signed <see cref="TokenRequest"/>, <see cref="TokenDetails"/> or a token string.</param>
        public AuthCallbackResult(object tokenCompatibleObject)
        {
            TokenCompatibleObject = tokenCompatibleObject;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthCallbackResult"/> class.
        /// </summary>
        /// <param name="tokenCompatibleObject">a signed <see cref="TokenRequest"/>, <see cref="TokenDetails"/> or a token string.</param>
        /// <param name="errorInfo">
        /// Where possible exceptions should be handled in the Authcallback and an ErrorInfo set.
        /// A null ErrorInfo indicates to Ably there were no errors.
        /// </param>
        public AuthCallbackResult(object tokenCompatibleObject, ErrorInfo errorInfo)
        {
            TokenCompatibleObject = tokenCompatibleObject;
            ErrorInfo = errorInfo;
        }
    }
}
