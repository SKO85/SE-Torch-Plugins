using SKO.Torch.Plugins.Tweaks.Config.Mod;
using System.Xml.Serialization;

namespace SKO.Torch.Plugins.Tweaks.Config
{
    public class SafeZonesConfig : IConfigSection
    {
        [XmlAttribute] public bool RemoveEmptySafeZones { get; set; }

        [XmlAttribute] public int CheckIntervalSeconds { get; set; }

        public FactionSafeZonesMod FactionSafeZonesMod { get; set; } = new FactionSafeZonesMod();

        public void Validate()
        {
            if (CheckIntervalSeconds < 10) CheckIntervalSeconds = 10;

            FactionSafeZonesMod.Validate();
        }
    }
}