using System.Xml.Serialization;

namespace SKO.Torch.Plugins.Tweaks.Config
{
    public class ShowAreaConfig : IConfigSection
    {
        [XmlAttribute] public bool Enabled { get; set; }

        [XmlAttribute] public int AllowShowAreaIntervalSeconds { get; set; } = 300;

        [XmlAttribute] public bool RemoveOnlyWhenNoPlayersNearby { get; set; }

        [XmlAttribute] public bool DisableOnBuildAndRepair { get; set; }

        [XmlAttribute] public bool DisableOnAdvancedDrills { get; set; }

        [XmlAttribute] public bool Log { get; set; }

        public void Validate()
        {
            if (AllowShowAreaIntervalSeconds < 0) AllowShowAreaIntervalSeconds = 0;
        }
    }
}