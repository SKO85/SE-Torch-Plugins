using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Game.WorldEnvironment;
using Sandbox.ModAPI;
using SKO.Torch.Shared.Plugin;
using SKO.Torch.Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using VRageMath;

namespace SKO.Torch.Plugins.Tweaks.Modules
{
    public class SafeZoneModule : PluginExtensionModule<SKOTweaksPlugin>
    {
        private const double SecondsForRemove = 5;

        private static readonly ConcurrentDictionary<long, RemoveSafeZone> ScheduleRemoval =
            new ConcurrentDictionary<long, RemoveSafeZone>();

        private static readonly ConcurrentDictionary<long, RemoveSafeZone> ToRemove =
            new ConcurrentDictionary<long, RemoveSafeZone>();

        private Timer _checkTimer;

        public SafeZoneModule(SKOTweaksPlugin pluginInstance) : base(pluginInstance)
        {
        }

        protected override void InitializeModule()
        {
            // Timers.
            _checkTimer = new Timer(MainModule.Config.SafeZones.CheckIntervalSeconds * 1000);
            _checkTimer.Elapsed += OnCheckTimerElapsed;
        }

        public void SetInterval(int intervalSeconds)
        {
            if (_checkTimer != null) _checkTimer.Interval = intervalSeconds * 1000;
        }

        private void OnCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (MainModule.Config.PluginEnabled && MainModule.EntityManager != null)
                Fix(MainModule.EntityManager.GetOf<MySafeZone>().ToHashSet());
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

        private void Fix(HashSet<MySafeZone> safeZones)
        {
            // Fix FSZ Mod stuff.
            if (MainModule.Config.SafeZones.FactionSafeZonesMod.Enabled)
                FixFSZ(safeZones);

            // Fix other safe-zone stuff.
            FixOtherSafeZones(safeZones);

            // Check Queue.
            CheckQueue();
        }

        private bool IsEmptySafeZone(MySafeZone safeZone)
        {
            try
            {
                var safeZoneSphere = new BoundingSphereD(safeZone.PositionComp.GetPosition(), safeZone.Radius);
                var entitiesInSafeZone = MyAPIGateway.Entities.GetEntitiesInSphere(ref safeZoneSphere);
                if (entitiesInSafeZone.Any())
                {
                    var ignoreTypes = new HashSet<Type>
                    {
                        typeof(MySafeZone),
                        typeof(MyCharacter),
                        typeof(MyPlanet),
                        typeof(MyVoxelBase),
                        typeof(MyVoxelMap),
                        typeof(MyVoxelMaps),
                        typeof(MyEnvironmentSector),
                        typeof(MySafeZone)
                    };

                    var ignoreTypesStrings = new HashSet<string>
                    {
                        "Sandbox.Game.Entities.MyVoxelPhysics"
                    };

                    var nonSafeZoneItems = entitiesInSafeZone.Where(c =>
                        !ignoreTypes.Contains(c.GetType()) && !ignoreTypesStrings.Contains(c.GetType().FullName));
                    if (nonSafeZoneItems.Count() == 0) return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private void FixOtherSafeZones(HashSet<MySafeZone> safeZones)
        {
            foreach (var safeZone in safeZones)
                try
                {
                    // Skip if...
                    if (safeZone == null || ScheduleRemoval.ContainsKey(safeZone.EntityId) ||
                        ToRemove.ContainsKey(safeZone.EntityId)) continue;

                    // If not already being removed.
                    if (!safeZone.Closed)
                        if (MainModule.Config.SafeZones.RemoveEmptySafeZones)
                            if (IsEmptySafeZone(safeZone))
                                // Schedule removal.
                                ScheduleRemoval[safeZone.EntityId] = new RemoveSafeZone
                                {
                                    QueueTime = MyAPIGateway.Session.ElapsedPlayTime,
                                    RemoveTime = MyAPIGateway.Session.ElapsedPlayTime.Add(TimeSpan.FromSeconds(10)),
                                    SafeZone = safeZone,
                                    Faction = safeZone.Factions?.FirstOrDefault()
                                };
                }
                catch
                {
                }
        }

        private void FixFSZ(HashSet<MySafeZone> safeZones)
        {
            if (MainModule.Config.SafeZones.FactionSafeZonesMod.Enabled)
                foreach (var safeZone in safeZones)
                    try
                    {
                        if (safeZone != null)
                        {
                            if (ScheduleRemoval.ContainsKey(safeZone.EntityId) ||
                                ToRemove.ContainsKey(safeZone.EntityId)) continue;

                            var safeZoneName = safeZone.DisplayName;

                            if (safeZoneName.ToLowerInvariant().Trim().StartsWith("(fsz)"))
                            {
                                if (safeZone.SafeZoneBlockId > 0) continue;

                                var faction = safeZone.Factions.FirstOrDefault();
                                if (faction == null)
                                {
                                    safeZone.Close();
                                    if (MainModule.Config.SafeZones.FactionSafeZonesMod.Log)
                                        SKOTweaksPlugin.Log.Warn(
                                            $"FSZ Mod Safe-Zone {safeZoneName} removed as there is no faction owning it.");
                                }
                                else
                                {
                                    // Check if players are online of this faction.
                                    var onlineMembers = FactionUtils.GetOnlineFactionMembers(faction);
                                    if (onlineMembers.Any())
                                    {
                                        ScheduleRemoval[safeZone.EntityId] = new RemoveSafeZone
                                        {
                                            SafeZone = safeZone,
                                            QueueTime = MyAPIGateway.Session.ElapsedPlayTime,
                                            RemoveTime = MyAPIGateway.Session.ElapsedPlayTime.Add(
                                                TimeSpan.FromSeconds(MainModule.Config.SafeZones.FactionSafeZonesMod
                                                    .RemoveCheckDelay)),
                                            Faction = faction
                                        };

                                        if (MainModule.Config.SafeZones.FactionSafeZonesMod.NotifyPlayers)
                                            foreach (var member in onlineMembers)
                                            {
                                                ChatUtils.SendTo(member.IdentityId,
                                                    $"SZ #{safeZone.EntityId} removal check.");
                                                ChatUtils.SendTo(member.IdentityId, "Standby....");
                                            }

                                        if (MainModule.Config.SafeZones.FactionSafeZonesMod.Log)
                                            SKOTweaksPlugin.Log.Warn(
                                                $"Safe-Zone {safeZoneName} queued for removal check. Faction: '{faction.Tag}'.");
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
        }

        private void CheckQueue()
        {
            if (!ScheduleRemoval.IsEmpty)
            {
                // Delete them now if time exceeded.
                var timeElapsed = MyAPIGateway.Session.ElapsedPlayTime;

                foreach (var entityId in ScheduleRemoval.Keys)
                    try
                    {
                        var item = ScheduleRemoval[entityId];
                        if (item.SafeZone == null)
                        {
                            ScheduleRemoval.TryRemove(entityId, out _);
                            continue;
                        }

                        var name = item.SafeZone.DisplayName;

                        if (timeElapsed >= item.RemoveTime)
                        {
                            if (!item.SafeZone.Closed)
                            {
                                // Check if empty.
                                if (IsEmptySafeZone(item.SafeZone))
                                {
                                    var sb = new StringBuilder();
                                    var factions = item.SafeZone.Factions.Select(c => c.Tag).ToList();
                                    factions.ForEach(c => sb.Append($"{c} "));

                                    item.SafeZone.Close();
                                    SKOTweaksPlugin.Log.Warn(
                                        $"Removed safe-zone #{item.SafeZone} as it is empty. Factions: {sb}, Position: {item.SafeZone.PositionComp.GetPosition()}");

                                    ScheduleRemoval.TryRemove(entityId, out _);
                                    continue;
                                }

                                // Check if we need to schedule removal.
                                var onlineMembers = FactionUtils.GetOnlineFactionMembers(item.Faction);
                                if (onlineMembers.Any())
                                {
                                    if (MainModule.Config.SafeZones.FactionSafeZonesMod.Log)
                                        SKOTweaksPlugin.Log.Warn(
                                            $"Safe-Zone {name} seems still to be active, so removing in {SecondsForRemove} seconds.");

                                    item.QueueTime = timeElapsed;
                                    ToRemove[entityId] = item;

                                    if (MainModule.Config.SafeZones.FactionSafeZonesMod.NotifyPlayers)
                                        foreach (var member in onlineMembers)
                                            ChatUtils.SendTo(member.IdentityId,
                                                $"SZ #{item.SafeZone.EntityId} removing in {SecondsForRemove}s.");
                                }
                                else
                                {
                                    if (MainModule.Config.SafeZones.FactionSafeZonesMod.Log)
                                        SKOTweaksPlugin.Log.Warn(
                                            $"Safe-Zone {name} removal skipped. No players online for faction {item.Faction.Tag}.");
                                }
                            }

                            ScheduleRemoval.TryRemove(entityId, out _);
                        }
                    }
                    catch
                    {
                    }
            }

            RemoveSafeZones();
        }

        private void RemoveSafeZones()
        {
            if (!ToRemove.IsEmpty)
            {
                var timeElapsed = MyAPIGateway.Session.ElapsedPlayTime;

                foreach (var entityId in ToRemove.Keys)
                {
                    var item = ToRemove[entityId];
                    if (timeElapsed.Subtract(item.QueueTime).TotalSeconds >= SecondsForRemove)
                    {
                        if (item.SafeZone != null && !item.SafeZone.Closed)
                        {
                            if (MainModule.Config.SafeZones.FactionSafeZonesMod.Log)
                                SKOTweaksPlugin.Log.Warn($"Safe-Zone {item.SafeZone.DisplayName} removed!");

                            item.SafeZone.Close();
                        }

                        ToRemove.TryRemove(entityId, out _);
                    }
                }
            }
        }

        private struct RemoveSafeZone
        {
            public MySafeZone SafeZone;
            public TimeSpan QueueTime;
            public MyFaction Faction;
            public TimeSpan RemoveTime;
        }
    }
}