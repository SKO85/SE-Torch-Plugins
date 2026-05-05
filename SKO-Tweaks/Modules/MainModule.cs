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
            // Subscribe before InitConfig() runs so the initial LoadConfig() invocation also reaches OnConfigLoaded.
            ConfigLoaded += OnConfigLoaded;

            SafeZoneModule = new SafeZoneModule(pluginInstance);
            ShareInertiaTensorModule = new ShareInertiaTensorModule(pluginInstance);
            DisableConnectorThrowOutModule = new DisableConnectorThrowOutModule(pluginInstance);
        }

        protected override void InitializeModule()
        {
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
            DisableConnectorThrowOutModule?.ApplyConfig(config.DisableConnectorThrowOut.DisableTimerSeconds);
        }

        public override void Start()
        {
            SafeZoneModule?.Start();
            ShareInertiaTensorModule?.Start();
            DisableConnectorThrowOutModule?.Start();
        }

        public override void Stop()
        {
            // Don't unsubscribe ConfigLoaded here — the subscription is set up in the constructor
            // (lives with the MainModule instance) so that subsequent !reload commands still reach
            // OnConfigLoaded after a session restart.
            SafeZoneModule?.Stop();
            ShareInertiaTensorModule?.Stop();
            DisableConnectorThrowOutModule?.Stop();
            base.Stop();
        }
    }
}