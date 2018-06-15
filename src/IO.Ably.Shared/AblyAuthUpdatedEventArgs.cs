using System;

namespace IO.Ably
{
    internal class AblyAuthUpdatedEventArgs : EventArgs
    {
        public TokenDetails Token { get; set; }
    }
}