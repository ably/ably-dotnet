using System.ComponentModel;

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
        [Description("None, no authentication parameters")]
        None = 0,

        [Description("Token auth with callback")]
        Callback,

        [Description("Token auth with URL")]
        Url,

        [Description("Token auth with client-side signing")]
        Signing,

        [Description("Token auth with supplied token only")]
        JustToken
    }
}