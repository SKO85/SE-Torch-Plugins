using System;
using System.Collections.Generic;

namespace SKO.Pack.Modules
{
    public class PingWatchPlayerData
    {
        public ulong SteamId { get; set; }
        public int Connections { get; set; } = 0;
        public List<short> Pings { get; set; } = new List<short>();
        public List<DateTime> Warnings { get; set; } = new List<DateTime>();
        public double LastAveragePing { get; set; } = 0;
    }
}