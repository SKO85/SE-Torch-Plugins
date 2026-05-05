using Torch;

namespace SKO.Pack.Modules
{
    public class PingWatchConfig : ViewModel
    {
        private int _checkIntervalMin = 1;

        public bool Enabled { get; set; } = true;
        public bool ShowLogWarnings { get; set; } = true;
        public int MaxPing { get; set; } = 250;
        public int MaxReconnectionsAllowed { get; set; } = 2;
        public int MaxProbes { get; set; } = 5;
        public int MaxWarnings { get; set; } = 10;

        public int CheckIntervalMin
        {
            get => _checkIntervalMin;
            set => _checkIntervalMin = value < 1 ? 1 : value;
        }

        public bool KickPlayer { get; set; } = true;
    }
}