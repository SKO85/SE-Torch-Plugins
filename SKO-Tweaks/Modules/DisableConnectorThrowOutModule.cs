using System;
using SKO.Torch.Shared.Plugin;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using IMyShipConnector = Sandbox.ModAPI.IMyShipConnector;

namespace SKO.Torch.Plugins.Tweaks.Modules
{
    public class DisableConnectorThrowOutModule : PluginExtensionModule<SKOTweaksPlugin>
    {
        private Timer _checkTimer;
        private bool _started;

        public DisableConnectorThrowOutModule(SKOTweaksPlugin pluginInstance) : base(pluginInstance)
        {
        }

        protected override void InitializeModule()
        {
            ApplyConfig(MainModule.Config.DisableConnectorThrowOut.DisableTimerSeconds);
        }

        /// <summary>
        ///     Re-applies the timer interval / on-off state from config. Safe to call from <c>!sko-tweaks config reload</c>.
        /// </summary>
        public void ApplyConfig(int disableTimerSeconds)
        {
            if (disableTimerSeconds <= 0)
            {
                _checkTimer?.Stop();
                return;
            }

            if (_checkTimer == null)
            {
                _checkTimer = new Timer();
                _checkTimer.Elapsed += OnCheckTimerElapsed;
            }

            _checkTimer.Interval = TimeSpan.FromSeconds(disableTimerSeconds).TotalMilliseconds;
            if (_started) _checkTimer.Start();
        }

        public override void Start()
        {
            _started = true;
            _checkTimer?.Start();
        }

        public override void Stop()
        {
            _started = false;
            _checkTimer?.Stop();
            base.Stop();
        }

        private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Timer.Elapsed runs on a ThreadPool thread; marshal game-state work to the main thread.
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                if (MainModule.Config.PluginEnabled && MainModule.EntityManager != null)
                    Fix(MainModule.EntityManager.GetOf<MyCubeGrid>().ToHashSet());
            });
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
