using Sandbox.Game.Entities;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;

namespace SKO.Torch.Shared.Extensions
{
    public static class MyCubeGridExtensions
    {
        public static IEnumerable<T> GetFatBlocks<T>(this MyCubeGrid grid)
            where T : class, IMyCubeBlock
        {
            return grid.GetBlocks()
                .Select(c => c?.FatBlock as T)
                .Where(b => b != null);
        }
    }
}