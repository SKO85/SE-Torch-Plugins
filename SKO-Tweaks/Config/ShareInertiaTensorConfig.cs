using System.Xml.Serialization;

namespace SKO.Torch.Plugins.Tweaks.Config
{
    public class ShareInertiaTensorConfig : IConfigSection
    {
        [XmlAttribute] public bool Enabled { get; set; }

        [XmlAttribute] public EnabledStateEnum Pistons { get; set; } = EnabledStateEnum.PlayerDefined;

        [XmlAttribute] public EnabledStateEnum Rotors { get; set; } = EnabledStateEnum.PlayerDefined;

        [XmlAttribute] public EnabledStateEnum Hinges { get; set; } = EnabledStateEnum.PlayerDefined;

        [XmlAttribute] public bool Log { get; set; }

        public void Validate()
        {
        }
    }

    public enum EnabledStateEnum
    {
        PlayerDefined,
        AlwaysEnabled,
        AlwaysDisabled,
        EnableAfterRestart,
        DisableAfterRestart
    }
}