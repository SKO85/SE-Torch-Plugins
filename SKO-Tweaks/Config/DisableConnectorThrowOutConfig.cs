using System.Xml.Serialization;

namespace SKO.Torch.Plugins.Tweaks.Config
{
    public class DisableConnectorThrowOutConfig : IConfigSection
    {
        [XmlAttribute] public int DisableTimerMinutes { get; set; } = 0;
        [XmlAttribute] public bool Log { get; set; }

        public void Validate()
        {
            if (DisableTimerMinutes < 0) DisableTimerMinutes = 0;
        }
    }
}
