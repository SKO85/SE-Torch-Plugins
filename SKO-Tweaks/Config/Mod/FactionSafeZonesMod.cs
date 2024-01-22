using System.Xml.Serialization;

namespace SKO.Torch.Plugins.Tweaks.Config.Mod
{
    public class FactionSafeZonesMod : IConfigSection
    {
        [XmlAttribute] public bool Enabled { get; set; }

        [XmlAttribute] public int RemoveCheckDelay { get; set; } = 65;

        [XmlAttribute] public bool NotifyPlayers { get; set; } = true;

        [XmlAttribute] public bool Log { get; set; } = true;

        public void Validate()
        {
            if (RemoveCheckDelay < 0)
                RemoveCheckDelay = 0;
        }
    }
}