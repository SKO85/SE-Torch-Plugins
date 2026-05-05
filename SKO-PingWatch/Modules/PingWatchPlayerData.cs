using System;
using System.Collections.Generic;

namespace SKO.Pack.Modules
{
    public class PingWatchPlayerData
    {
        public ulong SteamId { get; set; }
        public int Connections { get; set; } = 0;

        // Pings and Warnings are touched from the watcher loop and the warn/disconnect Task —
        // callers MUST take the lock on the owning PingWatchPlayerData instance before reading
        // or writing these lists, since List<T> is not thread-safe.
        public List<short> Pings { get; set; } = new List<short>();
        public List<DateTime> Warnings { get; set; } = new List<DateTime>();
        public double LastAveragePing { get; set; } = 0;
    }
}