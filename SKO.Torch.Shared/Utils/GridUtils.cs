using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace SKO.Torch.Shared.Utils
{
    public static class GridUtils
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static int GetPCU(IMyCubeGrid grid, bool includeSubGrids = false, bool includeConnectorDocked = false)
        {
            var pcu = 0;
            if (grid != null && grid.Physics != null)
            {
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                foreach (var block in blocks) pcu += BlockUtils.GetPCU(block as MySlimBlock);

                if (includeSubGrids)
                {
                    var subGrids = GetSubGrids(grid, includeConnectorDocked);
                    if (subGrids != null)
                        foreach (var subGrid in subGrids)
                            pcu += GetPCU(subGrid);
                }
            }

            return pcu;
        }

        public static List<IMyCubeGrid> GetSubGrids(IMyCubeGrid grid, bool includeConnectorDocked = false)
        {
            if (grid != null && grid.Physics != null)
            {
                var list = new List<IMyCubeGrid>();

                var gridLinkType = GridLinkTypeEnum.Mechanical;

                if (includeConnectorDocked) gridLinkType = GridLinkTypeEnum.Physical;

                // Get sub-grids.
                MyAPIGateway.GridGroups.GetGroup(grid, gridLinkType, list);

                if (list != null)
                    if (list.Count > 0)
                    {
                        var self = list.Where(c => c.EntityId == grid.EntityId).FirstOrDefault();
                        if (self != null) list.Remove(self);
                    }

                return list;
            }

            return null;
        }

        public static List<IMySlimBlock> GetBlocks<T>(IMyCubeGrid grid)
        {
            var blocks = new List<IMySlimBlock>();
            if (grid != null && grid.Physics != null)
                grid.GetBlocks(blocks, b => b.FatBlock != null && (b is T || b.FatBlock is T));
            return blocks;
        }

        public static void SetBlocksEnabled<T>(IMyCubeGrid grid, bool enabled, bool update = false)
        {
            var blocks = GetBlocks<T>(grid);
            foreach (var item in blocks)
                if (item.FatBlock is IMyFunctionalBlock)
                    (item as IMyFunctionalBlock).Enabled = enabled;

            if (update) (grid as MyCubeGrid).RaiseGridChanged();
        }

        public static ConcurrentBag<List<MyCubeGrid>> FindGridList(long playerId, bool includeConnectedGrids)
        {
            var grids = new ConcurrentBag<List<MyCubeGrid>>();

            if (includeConnectedGrids)
                Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group =>
                {
                    var gridList = new List<MyCubeGrid>();

                    foreach (var groupNodes in group.Nodes)
                    {
                        var grid = groupNodes.NodeData;

                        if (grid.Physics == null)
                            continue;

                        gridList.Add(grid);
                    }

                    if (IsPlayerIdCorrect(playerId, gridList))
                        grids.Add(gridList);
                });
            else
                Parallel.ForEach(MyCubeGridGroups.Static.Mechanical.Groups, group =>
                {
                    var gridList = new List<MyCubeGrid>();

                    foreach (var groupNodes in group.Nodes)
                    {
                        var grid = groupNodes.NodeData;

                        if (grid.Physics == null)
                            continue;

                        gridList.Add(grid);
                    }

                    if (IsPlayerIdCorrect(playerId, gridList))
                        grids.Add(gridList);
                });

            return grids;
        }

        public static List<MyCubeGrid> FindGridList(string gridNameOrEntityId, MyCharacter character,
            bool includeConnectedGrids)
        {
            var grids = new List<MyCubeGrid>();

            if (gridNameOrEntityId == null && character == null)
                return new List<MyCubeGrid>();

            if (includeConnectedGrids)
            {
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

                if (gridNameOrEntityId == null)
                    groups = FindLookAtGridGroup(character);
                else
                    groups = FindGridGroup(gridNameOrEntityId);

                if (groups.Count > 1)
                    return null;

                foreach (var group in groups)
                    foreach (var node in group.Nodes)
                    {
                        var grid = node.NodeData;

                        if (grid.Physics == null)
                            continue;

                        grids.Add(grid);
                    }
            }
            else
            {
                ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups;

                if (gridNameOrEntityId == null)
                    groups = FindLookAtGridGroupMechanical(character);
                else
                    groups = FindGridGroupMechanical(gridNameOrEntityId);

                if (groups.Count > 1)
                    return null;

                foreach (var group in groups)
                    foreach (var node in group.Nodes)
                    {
                        var grid = node.NodeData;

                        if (grid.Physics == null)
                            continue;

                        grids.Add(grid);
                    }
            }

            return grids;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindGridGroup(string gridName)
        {
            var groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Physical.Groups, group =>
            {
                foreach (var groupNodes in group.Nodes)
                {
                    var grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    /* Gridname is wrong ignore */
                    if (!grid.DisplayName.Equals(gridName) && grid.EntityId + "" != gridName)
                        continue;

                    groups.Add(group);
                }
            });

            return groups;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindLookAtGridGroup(
            IMyCharacter controlledEntity)
        {
            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix =
                controlledEntity
                    .GetHeadMatrix(
                        true); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var group in MyCubeGridGroups.Static.Physical.Groups)
                foreach (var groupNodes in group.Nodes)
                {
                    IMyCubeGrid cubeGrid = groupNodes.NodeData;

                    if (cubeGrid != null)
                    {
                        if (cubeGrid.Physics == null)
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.
                        if (ray.Intersects(cubeGrid.WorldAABB).HasValue)
                        {
                            var hit = cubeGrid.RayCastBlocks(startPosition, endPosition);

                            if (hit.HasValue)
                            {
                                var distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();

                                if (list.TryGetValue(group, out var oldDistance))
                                {
                                    if (distance < oldDistance)
                                    {
                                        list.Remove(group);
                                        list.Add(group, distance);
                                    }
                                }
                                else
                                {
                                    list.Add(group, distance);
                                }
                            }
                        }
                    }
                }

            var bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();

            if (list.Count == 0)
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> FindGridGroupMechanical(
            string gridName)
        {
            var groups = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group>();
            Parallel.ForEach(MyCubeGridGroups.Static.Mechanical.Groups, group =>
            {
                foreach (var groupNodes in group.Nodes)
                {
                    var grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    /* Gridname is wrong ignore */
                    if (!grid.DisplayName.Equals(gridName) && grid.EntityId + "" != gridName)
                        continue;

                    groups.Add(group);
                }
            });

            return groups;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group>
            FindLookAtGridGroupMechanical(IMyCharacter controlledEntity)
        {
            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix =
                controlledEntity
                    .GetHeadMatrix(
                        true); // dead center of player cross hairs, or the direction the player is looking with ALT.
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var group in MyCubeGridGroups.Static.Mechanical.Groups)
                foreach (var groupNodes in group.Nodes)
                {
                    IMyCubeGrid cubeGrid = groupNodes.NodeData;

                    if (cubeGrid != null)
                    {
                        if (cubeGrid.Physics == null)
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.
                        if (ray.Intersects(cubeGrid.WorldAABB).HasValue)
                        {
                            var hit = cubeGrid.RayCastBlocks(startPosition, endPosition);

                            if (hit.HasValue)
                            {
                                var distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();

                                if (list.TryGetValue(group, out var oldDistance))
                                {
                                    if (distance < oldDistance)
                                    {
                                        list.Remove(group);
                                        list.Add(group, distance);
                                    }
                                }
                                else
                                {
                                    list.Add(group, distance);
                                }
                            }
                        }
                    }
                }

            var bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group>();

            if (list.Count == 0)
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }

        public static ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> FindLookAtGridGroup(
            IMyCharacter controlledEntity, long playerId, bool factionFixEnabled)
        {
            const float range = 5000;
            Matrix worldMatrix;
            Vector3D startPosition;
            Vector3D endPosition;

            worldMatrix = controlledEntity.GetHeadMatrix(true);
            startPosition = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            endPosition = worldMatrix.Translation + worldMatrix.Forward * (range + 0.5f);

            var list = new Dictionary<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group, double>();
            var ray = new RayD(startPosition, worldMatrix.Forward);

            foreach (var group in MyCubeGridGroups.Static.Physical.Groups)
                foreach (var groupNodes in group.Nodes)
                {
                    var cubeGrid = groupNodes.NodeData;

                    if (cubeGrid != null)
                    {
                        if (cubeGrid.Physics == null)
                            continue;

                        /* We are not the server and playerId is not owner */
                        if (playerId != 0 && !OwnershipCorrect(cubeGrid, playerId, factionFixEnabled))
                            continue;

                        // check if the ray comes anywhere near the Grid before continuing.
                        if (ray.Intersects(cubeGrid.PositionComp.WorldAABB).HasValue)
                        {
                            var hit = cubeGrid.RayCastBlocks(startPosition, endPosition);

                            if (hit.HasValue)
                            {
                                var distance = (startPosition - cubeGrid.GridIntegerToWorld(hit.Value)).Length();

                                if (list.TryGetValue(group, out var oldDistance))
                                {
                                    if (distance < oldDistance)
                                    {
                                        list.Remove(group);
                                        list.Add(group, distance);
                                    }
                                }
                                else
                                {
                                    list.Add(group, distance);
                                }
                            }
                        }
                    }
                }

            var bag = new ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group>();

            if (list.Count == 0)
                return bag;

            // find the closest Entity.
            var item = list.OrderBy(f => f.Value).First();
            bag.Add(item.Key);

            return bag;
        }

        public static bool OwnershipCorrect(MyCubeGrid grid, long playerId, bool checkFactions)
        {
            /* If Player is owner we are totally fine and can allow it */
            if (grid.BigOwners.Contains(playerId))
                return true;

            /* If he is not owner and we dont want to allow checks for faction members... then prohibit */
            if (!checkFactions)
                return false;

            /* If checks for faction are allowed grab owner and see if factions are equal */
            var gridOwner = OwnershipUtils.GetOwner(grid);

            return FactionUtils.HavePlayersSameFaction(playerId, gridOwner);
        }

        private static bool IsPlayerIdCorrect(long playerId, List<MyCubeGrid> gridList)
        {
            MyCubeGrid biggestGrid = null;

            foreach (var grid in gridList)
                if (biggestGrid == null || biggestGrid.BlocksCount < grid.BlocksCount)
                    biggestGrid = grid;

            /* No biggest grid should not be possible, unless the gridgroup only had projections -.- just skip it. */
            if (biggestGrid == null)
                return false;

            var hasOwners = biggestGrid.BigOwners.Count != 0;

            if (!hasOwners)
            {
                if (playerId != 0L)
                    return false;

                return true;
            }

            return playerId == biggestGrid.BigOwners[0];
        }

        public static ConcurrentBag<List<MyCubeGrid>> GetGrids()
        {
            var grids = new ConcurrentBag<List<MyCubeGrid>>();

            Parallel.ForEach(MyCubeGridGroups.Static.Mechanical.Groups, group =>
            {
                var gridList = new List<MyCubeGrid>();

                foreach (var groupNodes in group.Nodes)
                {
                    var grid = groupNodes.NodeData;

                    if (grid.Physics == null)
                        continue;

                    gridList.Add(grid);
                }

                grids.Add(gridList);
            });

            return grids;
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

        public static bool MatchesGridNameOrIdWithWildcard(MyCubeGrid grid, string nameOrId)
        {
            if (nameOrId == null)
                return true;

            var gridName = grid.DisplayName;

            var regex = WildCardToRegular(nameOrId);

            if (Regex.IsMatch(gridName, regex))
                return true;

            var entityIdAsString = grid.EntityId + "";

            if (Regex.IsMatch(entityIdAsString, regex))
                return true;

            return false;
        }

        public static bool MatchesGridNameOrId(MyCubeGrid grid, string nameOrId)
        {
            if (nameOrId == null)
                return true;

            var gridName = grid.DisplayName;

            if (gridName.Equals(nameOrId))
                return true;

            var entityIdAsString = grid.EntityId + "";

            if (entityIdAsString.Equals(nameOrId))
                return true;

            return false;
        }

        public static MyCubeGrid GetBiggestGridInGroup(IEnumerable<MyCubeGrid> grids)
        {
            MyCubeGrid biggestGrid = null;

            foreach (var grid in grids)
            {
                if (grid.Physics == null)
                    continue;

                if (biggestGrid == null || biggestGrid.BlocksCount < grid.BlocksCount)
                    biggestGrid = grid;
            }

            return biggestGrid;
        }

        public static bool Repair(MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group)
        {
            foreach (var groupNodes in group.Nodes)
            {
                var grid = groupNodes.NodeData;

                var gridOwner = OwnershipUtils.GetOwner(grid);

                var blocks = grid.GetBlocks();
                foreach (var block in blocks)
                {
                    var owner = block.OwnerId;
                    if (owner == 0)
                        owner = gridOwner;

                    if (block.CurrentDamage > 0 || block.HasDeformation)
                    {
                        block.ClearConstructionStockpile(null);
                        block.IncreaseMountLevel(block.MaxIntegrity, owner, null, 10000, true);

                        var cubeBlock = block.FatBlock;

                        if (cubeBlock != null)
                        {
                            grid.ChangeOwnerRequest(grid, cubeBlock, 0, MyOwnershipShareModeEnum.Faction);
                            if (owner != 0)
                                grid.ChangeOwnerRequest(grid, cubeBlock, owner, MyOwnershipShareModeEnum.Faction);
                        }
                    }
                }
            }

            return true;
        }

        public static void TransferBlocksToBigOwner(HashSet<long> removedPlayers)
        {
            foreach (var entity in MyEntities.GetEntities())
            {
                if (!(entity is MyCubeGrid grid))
                    continue;

                var newOwner = grid.BigOwners.FirstOrDefault();

                /* If new owner is nobody we share with all */
                var share = newOwner == 0 ? MyOwnershipShareModeEnum.All : MyOwnershipShareModeEnum.Faction;

                foreach (var block in grid.GetFatBlocks())
                {
                    /* Nobody and players which werent deleted are ignored */
                    if (block.OwnerId == 0 || !removedPlayers.Contains(block.OwnerId))
                        continue;

                    grid.ChangeOwnerRequest(grid, block, 0, MyOwnershipShareModeEnum.Faction);
                    if (newOwner != 0)
                        grid.ChangeOwnerRequest(grid, block, newOwner, MyOwnershipShareModeEnum.Faction);
                }
            }
        }
    }
}