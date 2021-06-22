namespace IO.Ably.Push
{
    internal class RegistrationToken
    {
        /// <summary>
        /// FGM or GCM for Google.
        /// </summary>
        public string Type { get; set; }

        public string Token { get; set; }

        public RegistrationToken(string type, string token)
        {
            Type = type;
            Token = token;
        }

        public override string ToString()
        {
            return $"RegistrationToken: Type = {Type}, Token = {Token}";
        }
    }
}
