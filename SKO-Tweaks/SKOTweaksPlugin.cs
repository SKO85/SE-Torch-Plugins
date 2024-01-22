using NLog;
using SKO.Torch.Plugins.Tweaks.Modules;
using SKO.Torch.Shared.Managers.Entity;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Server;
using Torch.Session;
using VRage.ModAPI;

namespace SKO.Torch.Plugins.Tweaks
{
    public class SKOTweaksPlugin : TorchPluginBase
    {
        private static TorchSessionManager SessionManager;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static MainModule Module;

        public static EntityExpirationCacheManager<IMyEntity> ExpireCache =
            new EntityExpirationCacheManager<IMyEntity>();

        public TorchServer TorchServer;
        public static SKOTweaksPlugin Instance { get; private set; }

        public override void Init(ITorchBase torch)
        {
            // Set this instance.
            Instance = this;

            // Init Torch plugin.
            base.Init(torch);

            // Set Torch Server instance.
            TorchServer = (TorchServer)torch;

            // Set Tracker.
            Module = new MainModule(this, Constants.ConfigFileName);

            // Get the Session Manager.
            SessionManager = Torch.Managers.GetManager<TorchSessionManager>();

            // Handle Session State changed.
            if (SessionManager != null)
                SessionManager.SessionStateChanged += SessionManager_SessionStateChanged;
        }

        private void SessionManager_SessionStateChanged(ITorchSession session, TorchSessionState newState)
        {
            if (newState == TorchSessionState.Unloading)
                OnUnloading();
            else if (newState == TorchSessionState.Loaded) OnLoaded();
        }

        private void OnLoaded()
        {
            // Initialize module.
            Module?.Init();

            // Start the module.
            Module?.Start();
        }

        private void OnUnloading()
        {
            Module?.Stop();
        }
    }
}