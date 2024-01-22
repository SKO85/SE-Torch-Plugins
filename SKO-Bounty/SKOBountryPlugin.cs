using NLog;
using SKO.Bounty.Modules;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Server;
using Torch.Session;

namespace SKO.Bounty
{
    public class SKOBountyPlugin : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static TorchSessionManager SessionManager;
        public TorchServer TorchServer;
        public static SKOBountyPlugin Instance { get; private set; }

        public override void Init(ITorchBase torch)
        {
            Instance = this;

            base.Init(torch);

            // Set Torch Server instance.
            TorchServer = (TorchServer)torch;

            // Config.
            BountyModule.LoadConfig();
            BountyModule.LoadContracts();
            BountyModule.SaveConfig();

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
            BountyModule.InitializeEvents();
        }

        private void OnUnloading()
        {
        }
    }
}