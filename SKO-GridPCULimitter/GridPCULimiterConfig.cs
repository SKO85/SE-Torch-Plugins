using Torch;

namespace SKO.GridPCULimiter
{
    public class GridPCULimiterConfig : ViewModel
    {
        public bool Enabled { get; set; } = false;
        public int MaxGridPCU { get; set; } = 10000;
        public bool IgnoreNPCGrids { get; set; } = false;

        public bool AllowProjection { get; set; } = false;
        public bool AllowMerge { get; set; } = false;

        public bool IncludeConnectedGridsPCU { get; set; } = true;
        public int MaxNumberOfConnectedGrids { get; set; } = 2;
        public bool DamageConnectors { get; set; } = true;

        public int DisableWeldersWithinMeters { get; set; } = 30;
    }
}