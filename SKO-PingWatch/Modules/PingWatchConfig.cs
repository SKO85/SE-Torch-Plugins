using Torch;

namespace SKO.Pack.Modules
{
    public class PingWatchConfig : ViewModel
    {
        public bool Enabled { get; set; } = true;
        public bool ShowLogWarnings { get; set; } = true;
        public int MaxPing { get; set; } = 250;
        public int MaxReconnectionsAllowed { get; set; } = 2;
        public int MaxProbes { get; set; } = 5;
        public int MaxWarnings { get; set; } = 10;
        public int CheckIntervalMin { get; set; } = 1;
        public bool KickPlayer { get; set; } = true;
    }
}