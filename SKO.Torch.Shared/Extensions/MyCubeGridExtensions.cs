using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace SKO.Torch.Shared.Extensions
{
    public static class MyCubeGridExtensions
    {
        public static IEnumerable<T> GetFatBlocks<T>(this MyCubeGrid grid)
            where T : IMyCubeBlock
        {
            return grid.GetBlocks().Where(c => c?.FatBlock != null && c?.FatBlock is T)
                .Select(c => (T)Convert.ChangeType(c.FatBlock, typeof(T)));
        }
    }
}