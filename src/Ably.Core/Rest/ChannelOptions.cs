namespace IO.Ably.Rest
{
    public class ChannelOptions
    {
        public bool Encrypted { get; set; }
        public CipherParams CipherParams { get; set; }

        public ChannelOptions()
        {
            
        }

        public ChannelOptions(CipherParams @params)
        {
            Encrypted = true;
            CipherParams = @params;
        }
    }
}