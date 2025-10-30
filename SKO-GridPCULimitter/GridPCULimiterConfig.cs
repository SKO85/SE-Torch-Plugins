using System.Collections.ObjectModel;
using Torch;

namespace SKO.GridPCULimiter
{
    public class GridPCULimiterConfig : ViewModel
    {
        private bool _enabled = false;
        private int _maxGridPCU = 10000;
        private bool _ignoreNPCGrids = false;
        private bool _allowProjection = false;
        private bool _allowMerge = false;
        private bool _includeConnectedGridsPCU = true;
        private int _maxNumberOfConnectedGrids = 2;
        private bool _damageConnectors = true;
        private int _disableWeldersWithinMeters = 30;
        private ObservableCollection<ulong> _exemptSteamIds = new ObservableCollection<ulong>();

        public bool Enabled { get => _enabled; set => SetValue(ref _enabled, value); }
        public int MaxGridPCU { get => _maxGridPCU; set => SetValue(ref _maxGridPCU, value); }
        public bool IgnoreNPCGrids { get => _ignoreNPCGrids; set => SetValue(ref _ignoreNPCGrids, value); }

        public bool AllowProjection { get => _allowProjection; set => SetValue(ref _allowProjection, value); }
        public bool AllowMerge { get => _allowMerge; set => SetValue(ref _allowMerge, value); }

        public bool IncludeConnectedGridsPCU { get => _includeConnectedGridsPCU; set => SetValue(ref _includeConnectedGridsPCU, value); }
        public int MaxNumberOfConnectedGrids { get => _maxNumberOfConnectedGrids; set => SetValue(ref _maxNumberOfConnectedGrids, value); }
        public bool DamageConnectors { get => _damageConnectors; set => SetValue(ref _damageConnectors, value); }

        public int DisableWeldersWithinMeters { get => _disableWeldersWithinMeters; set => SetValue(ref _disableWeldersWithinMeters, value); }

        public ObservableCollection<ulong> ExemptSteamIds { get => _exemptSteamIds; set => SetValue(ref _exemptSteamIds, value); }
    }
}