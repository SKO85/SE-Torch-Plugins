using SKO.Torch.Shared.Plugin;

namespace SKO.Torch.Plugins.Tweaks.Config
{
    public class PluginConfig : PluginConfigBase
    {
        public bool FixDuplicateEntityIds { get; set; } = false;

        public SafeZonesConfig SafeZones { get; set; } = new SafeZonesConfig();
        public ShareInertiaTensorConfig ShareInertiaTensor { get; set; } = new ShareInertiaTensorConfig();
        public ShowAreaConfig ShowArea { get; set; } = new ShowAreaConfig();
        public DisableConnectorThrowOutConfig DisableConnectorThrowOut = new DisableConnectorThrowOutConfig();

        public override void Validate()
        {
            SafeZones.Validate();
            ShareInertiaTensor.Validate();
            ShowArea.Validate();
            DisableConnectorThrowOut.Validate();
        }
    }
}