namespace IO.Ably
{
    public enum AuthMethod : byte
    {
        Basic,
        Token
    }

    /// <summary>The library supports several token authentication methods, this enum lists those methods + descriptions.</summary>
    internal enum TokenAuthMethod : byte
    {
        None = 0,
        Callback,
        Url,
        Signing,
        JustToken
    }
}
