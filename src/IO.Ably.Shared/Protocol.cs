namespace IO.Ably
{
    public enum Protocol
    {
#if MSGPACK
        MsgPack = 0,
#endif
        Json = 1
    }
}
