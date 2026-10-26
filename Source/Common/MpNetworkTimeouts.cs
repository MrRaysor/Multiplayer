namespace Multiplayer.Common
{
    public static class MpNetworkTimeouts
    {
        // LiteNet transport timeout. Large mod lists can block the main thread (and starve the
        // network thread under memory pressure) for tens of seconds while loading or joining.
        public const int DisconnectTimeoutMs = 30000;
    }
}
