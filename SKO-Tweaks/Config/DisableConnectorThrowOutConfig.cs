using System.Xml.Serialization;

namespace SKO.Torch.Plugins.Tweaks.Config
{
    public class DisableConnectorThrowOutConfig : IConfigSection
    {
        [XmlAttribute] public int DisableTimerSeconds { get; set; } = 0;
        [XmlAttribute] public bool Log { get; set; }

        public void Validate()
        {
            if (DisableTimerSeconds < 0) DisableTimerSeconds = 0;

            if (DisableTimerSeconds > 0 && DisableTimerSeconds < 10)
            {
                DisableTimerSeconds = 10;
            }
        }
    }
}
