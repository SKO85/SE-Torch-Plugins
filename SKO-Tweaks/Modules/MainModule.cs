using SKO.Torch.Plugins.Tweaks.Config;
using SKO.Torch.Plugins.Tweaks.Utils;
using SKO.Torch.Shared.Managers.Entity;
using SKO.Torch.Shared.Plugin;
using VRage.Game.Entity;

namespace SKO.Torch.Plugins.Tweaks.Modules
{
    public class MainModule : PluginMainModule<PluginConfig, SKOTweaksPlugin>
    {
        public static EntityCacheManager<MyEntity> EntityManager;
        public static SafeZoneModule SafeZoneModule;
        public static ShareInertiaTensorModule ShareInertiaTensorModule;
        public static DisableConnectorThrowOutModule DisableConnectorThrowOutModule;

        public MainModule(SKOTweaksPlugin pluginInstance, string configFile = null) : base(pluginInstance, configFile)
        {
            SafeZoneModule = new SafeZoneModule(pluginInstance);
            ShareInertiaTensorModule = new ShareInertiaTensorModule(pluginInstance);
            DisableConnectorThrowOutModule = new DisableConnectorThrowOutModule(pluginInstance);
        }

        protected override void InitializeModule()
        {
            ConfigLoaded += OnConfigLoaded;

            if (Config.FixDuplicateEntityIds) EntityDuplicatesFix.Fix();

            // Initialize the entities cache manager.
            EntityManager = new EntityCacheManager<MyEntity>();

            // Init sub-modules.
            SafeZoneModule.Init();
            ShareInertiaTensorModule.Init();
            DisableConnectorThrowOutModule.Init();
        }

        private void OnConfigLoaded(PluginConfig config)
        {
            SafeZoneModule?.SetInterval(config.SafeZones.CheckIntervalSeconds);
        }

        public override void Start()
        {
            SafeZoneModule?.Start();
            ShareInertiaTensorModule?.Start();
            DisableConnectorThrowOutModule?.Start();
        }

        public override void Stop()
        {
            ConfigLoaded -= OnConfigLoaded;
            SafeZoneModule?.Stop();
            ShareInertiaTensorModule?.Stop();
            DisableConnectorThrowOutModule?.Stop();
            base.Stop();
        }
    }
}