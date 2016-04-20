namespace IO.Ably.Rest
{
    public class ChannelOptions
    {
        public bool Encrypted { get; private set; }
        public CipherParams CipherParams { get; private set; }

        public ChannelOptions(bool encrypted = false, CipherParams @params = null)
        {
            Encrypted = encrypted;
            CipherParams = @params;
        }

        public ChannelOptions(CipherParams @params)
        {
            Encrypted = true;
            CipherParams = @params;
        }
    }
}