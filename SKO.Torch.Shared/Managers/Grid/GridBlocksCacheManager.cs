using Sandbox.Game.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace SKO.Torch.Shared.Managers.Grid
{
    public static class GridBlocksCacheManager
    {
        public static ConcurrentDictionary<long, ConcurrentDictionary<long, IMyCubeBlock>> Blocks =
            new ConcurrentDictionary<long, ConcurrentDictionary<long, IMyCubeBlock>>();

        public static void Untrack(IMyCubeGrid grid)
        {
            if (Blocks.ContainsKey(grid.EntityId))
                try
                {
                    grid.OnBlockAdded -= Grid_OnBlockAdded;
                    grid.OnBlockRemoved -= Grid_OnBlockRemoved;

                    Blocks.TryRemove(grid.EntityId, out _);
                }
                catch
                {
                }
        }

        public static void Track(IMyCubeGrid grid)
        {
            if (grid == null || grid.Closed || grid.Physics == null)
                return;

            if (!Blocks.ContainsKey(grid.EntityId))
            {
                Blocks[grid.EntityId] = new ConcurrentDictionary<long, IMyCubeBlock>();

                // Add blocks.
                var items = new List<IMySlimBlock>();
                grid.GetBlocks(items, s => s?.FatBlock != null);
                foreach (var block in items) Blocks[grid.EntityId][block.FatBlock.EntityId] = block.FatBlock;

                // Add event listener.
                grid.OnBlockAdded += Grid_OnBlockAdded;
                grid.OnBlockRemoved += Grid_OnBlockRemoved;
            }
        }

        private static void Grid_OnBlockRemoved(IMySlimBlock obj)
        {
            if (obj?.FatBlock != null && Blocks.ContainsKey(obj.CubeGrid.EntityId))
                if (Blocks[obj.CubeGrid.EntityId].ContainsKey(obj.FatBlock.EntityId))
                    Blocks[obj.CubeGrid.EntityId].TryRemove(obj.FatBlock.EntityId, out _);
        }

        private static void Grid_OnBlockAdded(IMySlimBlock obj)
        {
            if (obj?.FatBlock != null && Blocks.ContainsKey(obj.CubeGrid.EntityId))
                if (!Blocks[obj.CubeGrid.EntityId].ContainsKey(obj.FatBlock.EntityId))
                    Blocks[obj.CubeGrid.EntityId][obj.FatBlock.EntityId] = obj.FatBlock;
        }

        public static Dictionary<long, T> GetBlocks<T>(long gridEntityId, string subTypeContains = null)
            where T : IMyCubeBlock
        {
            if (Blocks.ContainsKey(gridEntityId))
            {
                var grid = MyEntities.GetEntities().OfType<MyCubeGrid>()
                    .FirstOrDefault(c => c.EntityId == gridEntityId);
                if (grid != null)
                {
                    grid.RaiseGridChanged();
                    return grid.GetBlocks().Where(c => c.FatBlock != null).Select(c => c.FatBlock).OfType<T>().Where(
                            c => string.IsNullOrEmpty(subTypeContains) || (!string.IsNullOrEmpty(subTypeContains) &&
                                                                           c.BlockDefinition.SubtypeName.Contains(
                                                                               subTypeContains,
                                                                               StringComparison
                                                                                   .InvariantCultureIgnoreCase)))
                        .ToDictionary(c => c.EntityId, c => c);
                }
            }

            return new Dictionary<long, T>();
        }
    }
}