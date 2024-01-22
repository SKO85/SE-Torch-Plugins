using NLog;
using SKO.Pack.Modules;
using SKO.Torch.Shared.Utils;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Server;
using Torch.Session;

namespace SKO.PingWatch
{
    public class SKOPingWatchPlugin : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static TorchSessionManager SessionManager;
        private Modules.PingWatch PingWatch;
        public TorchServer TorchServer;
        public static SKOPingWatchPlugin Instance { get; private set; }

        public override void Init(ITorchBase torch)
        {
            Instance = this;

            base.Init(torch);

            // Set Torch Server instance.
            TorchServer = (TorchServer)torch;

            // Set PingWatch.
            PingWatch = new Modules.PingWatch();
            Modules.PingWatch.Config = ConfigUtils.Load<PingWatchConfig>(this, "SKOPingWatch.cfg");
            ConfigUtils.Save(this, Modules.PingWatch.Config, "SKOPingWatch.cfg");

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
            PingWatch.Initialize();
        }

        private void OnUnloading()
        {
            PingWatch.Clean();
        }
    }
}