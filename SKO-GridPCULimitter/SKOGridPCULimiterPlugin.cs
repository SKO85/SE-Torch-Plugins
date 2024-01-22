using System;
using System.Linq;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using SKO.Torch.Shared.Utils;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Server;
using Torch.Session;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using IMyProjector = Sandbox.ModAPI.IMyProjector;
using IMyShipConnector = Sandbox.ModAPI.IMyShipConnector;
using IMyShipWelder = Sandbox.ModAPI.IMyShipWelder;

namespace SKO.GridPCULimiter
{
    public class SKOGridPCULimiterPlugin : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static GridPCULimiterConfig Config;
        private static TorchSessionManager SessionManager;
        public TorchServer TorchServer;
        public static SKOGridPCULimiterPlugin Instance { get; private set; }

        public override void Init(ITorchBase torch)
        {
            Instance = this;

            base.Init(torch);

            // Set Torch Server instance.
            TorchServer = (TorchServer)torch;

            // Config
            Config = ConfigUtils.Load<GridPCULimiterConfig>(this, "SKOGridPCULimiter.cfg");
            ConfigUtils.Save(this, Config, "SKOGridPCULimiter.cfg");

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
            MyCubeGrids.BlockFunctional += OnBlockFunctional;
            MyVisualScriptLogicProvider.ConnectorStateChanged += OnConnectorStateChanged;
        }

        private void OnConnectorStateChanged(long entityId, long gridId, string entityName, string gridName,
            long otherEntityId, long otherGridId, string otherEntityName, string otherGridName, bool isConnected)
        {
            // Only check if state has changed to connected.
            if (Config.Enabled && Config.IncludeConnectedGridsPCU && isConnected)
            {
                var grid = MyAPIGateway.Entities.GetEntityById(gridId) as IMyCubeGrid;

                if (grid?.Physics != null)
                {
                    if (Config.IgnoreNPCGrids)
                    {
                        var ownerId = PlayerUtils.GetOwner(grid as MyCubeGrid);
                        if (PlayerUtils.IsNpc(ownerId)) return;
                    }

                    if (GridUtils.GetPCU(grid, true, Config.IncludeConnectedGridsPCU) > Config.MaxGridPCU)
                    {
                        var subGrids = GridUtils.GetSubGrids(grid, true);
                        var numOfConnectedSubGrids = 0;

                        if (subGrids != null)
                            foreach (var subGrid in subGrids)
                            {
                                var connectors = ((MyCubeGrid)subGrid).CubeBlocks
                                    .Where(b => b?.FatBlock is IMyShipConnector).ToList();

                                foreach (var block in connectors)
                                {
                                    var connector = block.FatBlock as IMyShipConnector;
                                    if (connector != null && connector.IsFunctional && connector.Enabled &&
                                        connector.Status == MyShipConnectorStatus.Connected &&
                                        connector.EntityId != entityId &&
                                        connector.EntityId != otherEntityId) numOfConnectedSubGrids++;
                                }
                            }

                        if (numOfConnectedSubGrids > 0 && numOfConnectedSubGrids % 2 == 0) numOfConnectedSubGrids /= 2;

                        if (numOfConnectedSubGrids + 1 > Config.MaxNumberOfConnectedGrids)
                            if (MyAPIGateway.Entities.GetEntityById(entityId) is IMyShipConnector connector)
                            {
                                // Disconnect the block and disable it.
                                connector.Disconnect();
                                connector.Enabled = false;

                                // Do damage to connector?
                                if (Config.DamageConnectors)
                                {
                                    var slimBlock = connector?.SlimBlock;
                                    slimBlock?.DoDamage(1500, MyDamageType.Rocket, true);
                                }

                                // Send message to players.
                                var playersNearby = PlayerUtils.GetPlayersInRadiusOfEntity(connector, 100);
                                if (playersNearby != null)
                                    foreach (var player in playersNearby)
                                        if (player != null)
                                            if (player?.IdentityId > 0)
                                            {
                                                SoundUtils.SendTo(player.IdentityId);
                                                ChatUtils.SendTo(player.IdentityId,
                                                    $"Connection not allowed! You're exceeding the total Grid PCU limit and the maximum number of {Config.MaxNumberOfConnectedGrids} connected grids has been reached.");
                                            }
                            }
                    }
                }
            }
        }

        private void OnBlockFunctional(MyCubeGrid cube, MySlimBlock block, bool isFinished)
        {
            if (Config.Enabled && cube != null && block?.FatBlock != null)
                try
                {
                    if (block.FatBlock.IsFunctional)
                    {
                        if (Config.IgnoreNPCGrids)
                        {
                            var ownerId = PlayerUtils.GetOwner(block.CubeGrid);
                            if (PlayerUtils.IsNpc(ownerId)) return;
                        }

                        var player = PlayerUtils.GetPlayer(block.BuiltBy);
                        if (player != null && PlayerUtils.IsAdmin(player) &&
                            PlayerUtils.IsPCULimitIgnored(player.SteamUserId)) return;

                        if (GridUtils.GetPCU(cube, true, Config.IncludeConnectedGridsPCU) >= Config.MaxGridPCU)
                        {
                            // Disable projects and welders if projecting to avoid spamming.
                            DisableProjectors(cube, block, player);

                            // Disable any welder around the block.
                            DisableWeldersWithinDistance(block);

                            // Send message to player.
                            if (block?.BuiltBy > 0)
                            {
                                SoundUtils.SendTo(block.BuiltBy);
                                ChatUtils.SendTo(block.BuiltBy, "Grid PCU Limit reached!");
                                ChatUtils.SendTo(block.BuiltBy, "You cannot weld to Functional.");
                                ChatUtils.SendTo(block.BuiltBy, "Block has been removed!");
                            }

                            // Remove the block.
                            cube.RemoveBlock(block);

                            // Sync changes.
                            cube.RaiseGridChanged();
                        }
                    }
                }
                catch (Exception)
                {
                }
        }

        private void DisableProjectors(MyCubeGrid cube, MySlimBlock block, IMyPlayer player)
        {
            var localProjectors = GridUtils.GetBlocks<IMyProjector>(cube);
            if (localProjectors.Count > 0 && localProjectors.Any(c => ((IMyProjector)c.FatBlock).Enabled))
            {
                if (player?.IdentityId > 0)
                {
                    SoundUtils.SendTo(player.IdentityId);
                    ChatUtils.SendTo(player.IdentityId, "Disabling Projector.");
                }

                foreach (var projector in localProjectors)
                    ((IMyProjector)projector.FatBlock).Enabled = false;

                if (player != null)
                {
                    if (player.IdentityId > 0) ChatUtils.SendTo(player.IdentityId, "Disabling Welders of all Grids.");

                    var gridGroups = GridUtils.FindGridList(player.IdentityId, true);
                    foreach (var gridGroup in gridGroups)
                    foreach (var grid in gridGroup)
                    {
                        var welders = GridUtils.GetBlocks<IMyShipWelder>(grid);
                        foreach (var welder in welders) ((IMyShipWelder)welder.FatBlock).Enabled = false;

                        grid.RaiseGridChanged();
                    }
                }
            }
        }

        private void DisableWeldersWithinDistance(MySlimBlock block)
        {
            // If radius is invalid, stop.
            if (Config.DisableWeldersWithinMeters <= 0 || block == null)
                return;

            // Search sphere for welders.
            var sphere = new BoundingSphereD(block.WorldPosition, Config.DisableWeldersWithinMeters);
            var list = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            if (list == null) return;

            // Get welders in the list of blocks.
            var welders = list.Where(c => c is IMyShipWelder welder && welder.Enabled).ToList();
            if (welders.Count <= 0) return;

            // Disable welder.
            foreach (var welder in welders.Cast<IMyShipWelder>())
                if (welder.Enabled)
                    welder.Enabled = false;
        }

        private void OnUnloading()
        {
            MyCubeGrids.BlockFunctional -= OnBlockFunctional;
            MyVisualScriptLogicProvider.ConnectorStateChanged -= OnConnectorStateChanged;
        }
    }
}