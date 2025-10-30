using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System.Linq;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace SKO.Torch.Shared.Utils
{
    public static class BlockUtils
    {
        public static int GetPCU(MySlimBlock block)
        {
            var pcuValue = 1;
            if (block.ComponentStack.IsFunctional)
                pcuValue = block.BlockDefinition.PCU;
            return pcuValue;
        }

        public static MyDefinitionId GetDefinitionId(string subTypeId)
        {
            var definitions = MyDefinitionManager.Static.GetAllDefinitions();
            var itemDefinition = definitions.FirstOrDefault(c => c.Id != null && c.Id.SubtypeName == subTypeId);
            return itemDefinition.Id;
        }

        public static bool PlayersNarby(IMyCubeBlock block, int radius)
        {
            if (block != null)
                try
                {
                    var sphere = new BoundingSphereD(block.GetPosition(), radius);
                    var entitiesInsphere = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                    var characters = entitiesInsphere.OfType<IMyCharacter>().Where(c => !c.IsDead && c.IsPlayer)
                        .Select(c => c as MyCharacter);
                    if (characters.Any())
                    {
                        var onlinePlayers = MyVisualScriptLogicProvider.GetOnlinePlayers();
                        var hasOnlinePlayers = characters.Any(c => onlinePlayers.Contains(c.GetPlayerIdentityId()));
                        if (hasOnlinePlayers) return true;
                    }
                }
                catch
                {
                }

            return false;
        }
    }
}