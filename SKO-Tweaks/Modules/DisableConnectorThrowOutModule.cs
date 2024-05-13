using System;
using SKO.Torch.Shared.Plugin;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Interfaces;
using IMyShipConnector = Sandbox.ModAPI.IMyShipConnector;

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
            if (MainModule.Config.DisableConnectorThrowOut.DisableTimerMinutes > 0)
            {
                _checkTimer = new Timer(TimeSpan.FromMinutes(MainModule.Config.DisableConnectorThrowOut.DisableTimerMinutes).TotalMilliseconds);
                _checkTimer.Elapsed += OnCheckTimerElapsed;
            }
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
                Fix(MainModule.EntityManager.GetOf<MyCubeGrid>().ToHashSet());
        }

        public void SetInterval(int intervalMinutes)
        {
            if (_checkTimer != null && intervalMinutes > 0)
            {
                _checkTimer.Interval = TimeSpan.FromMinutes(intervalMinutes).TotalMilliseconds;
            }
        }

        private void Fix(HashSet<MyCubeGrid> grids)
        {
            var connectors = new List<IMyShipConnector>();

            // Get connectors of the grid.
            foreach (var grid in grids)
            {
                var gridConnectors = grid.CubeBlocks.Where(b => b?.FatBlock is IMyShipConnector).ToList();
                if (gridConnectors != null)
                {
                    connectors.AddRange(gridConnectors.Select(block => block.FatBlock as IMyShipConnector).Where(c => c != null && c.ThrowOut).ToList());
                }
            }

            // If there is any connector to process...
            if (connectors.Any())
            {
                foreach (var connector in connectors)
                {
                    try
                    {
                        // Disable control panel settings.
                        connector.SetValueBool("ThrowOut", false);
                        connector.SetValueBool("CollectAll", false);

                        if (MainModule.Config.DisableConnectorThrowOut.Log)
                        {
                            SKOTweaksPlugin.Log.Warn($"Disabled ThrowOut for connector '{connector.CustomName}' of grid '{connector.CubeGrid.DisplayName}'. Grid EntityId: '{connector.CubeGrid.EntityId}'.");
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
