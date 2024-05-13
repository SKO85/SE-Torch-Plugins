using SKO.Torch.Shared.Plugin;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI.Interfaces;

namespace SKO.Torch.Plugins.Tweaks.Modules
{
    public class DisableConnectorThrowOutModule : PluginExtensionModule<SKOTweaksPlugin>
    {
        private Timer _checkTimer;

        public DisableConnectorThrowOutModule(SKOTweaksPlugin pluginInstance) : base(pluginInstance)
        {
        }

        protected override void InitializeModule()
        {
            // Timers.
            _checkTimer = new Timer(MainModule.Config.SafeZones.CheckIntervalSeconds * 1000);
            _checkTimer.Elapsed += OnCheckTimerElapsed;
        }

        public override void Start()
        {
            _checkTimer?.Start();
        }

        public override void Stop()
        {
            _checkTimer?.Stop();
            base.Stop();
        }

        private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (MainModule.Config.PluginEnabled && MainModule.EntityManager != null)
                Fix(MainModule.EntityManager.GetOf<MyShipConnector>().ToHashSet());
        }

        public void SetInterval(int intervalMinutes)
        {
            if (_checkTimer != null && intervalMinutes > 0)
            {
                _checkTimer.Interval = intervalMinutes * 60 * 1000;
            }
        }

        private void Fix(HashSet<MyShipConnector> connectors)
        {
            // Fix FSZ Mod stuff.
            if (MainModule.Config.DisableConnectorThrowOut.DisableTimerMinutes > 0)
            {
                if (connectors != null)
                {
                    var throwOutEnabledConnectors = connectors.Where(c => c.ThrowOut.Value).ToList();
                    if (throwOutEnabledConnectors.Any())
                    {
                        foreach (var connector in throwOutEnabledConnectors)
                        {
                            try
                            {
                                connector.SetValueBool("ThrowOut", false);

                                if (MainModule.Config.DisableConnectorThrowOut.Log) {
                                    SKOTweaksPlugin.Log.Warn($"Disabled ThrowOut for connector '{connector.CustomName}' of grid '{connector.CubeGrid.DisplayName}'.");
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                    }
                }
            }
        }
    }
}
