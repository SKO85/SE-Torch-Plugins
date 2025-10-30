using Sandbox.Game.Entities;
using System.Collections.Generic;

namespace SKO.Torch.Shared.Utils
{
    public class OwnershipUtils
    {
        public static long GetOwner(MyCubeGrid grid)
        {
            var gridOwnerList = grid.BigOwners;
            var ownerCnt = gridOwnerList.Count;
            var gridOwner = 0L;

            if (ownerCnt > 0 && gridOwnerList[0] != 0)
                return gridOwnerList[0];
            if (ownerCnt > 1)
                return gridOwnerList[1];

            return gridOwner;
        }

        public static Dictionary<long, BuildStats> FindBuildStatsPerPlayer()
        {
            var stats = new Dictionary<long, BuildStats>();

            foreach (var entity in MyEntities.GetEntities())
            {
                if (!(entity is MyCubeGrid grid))
                    continue;

                foreach (var block in grid.GetBlocks())
                {
                    var buildBy = block.BuiltBy;

                    if (!stats.TryGetValue(buildBy, out var statsForPlayer))
                    {
                        statsForPlayer = new BuildStats();
                        stats.Add(buildBy, statsForPlayer);
                    }

                    statsForPlayer.BlockCount++;
                    statsForPlayer.PcuCount += BlockUtils.GetPCU(block);
                }
            }

            return stats;
        }

        public class BuildStats
        {
            public int PcuCount { get; set; }
            public int BlockCount { get; set; }
        }
    }
}