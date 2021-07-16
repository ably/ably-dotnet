using Newtonsoft.Json.Linq;

namespace IO.Ably.Push
{
    /// <summary>
    /// Class used to hold registration tokens.
    /// </summary>
    public class RegistrationToken
    {
        /// <summary>
        /// FGM or GCM for Google and APNS for Apple.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Token value.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Constructs a new registration token instance.
        /// </summary>
        /// <param name="type">Token type.</param>
        /// <param name="token">Token value.</param>
        public RegistrationToken(string type, string token)
        {
            Type = type;
            Token = token;
        }

        /// <summary>
        /// Overrides to string to display Type and Token.
        /// </summary>
        /// <returns>Returns a string including Type and Token Value.</returns>
        public override string ToString()
        {
            return $"RegistrationToken: Type = {Type}, Token = {Token}";
        }
    }
}
