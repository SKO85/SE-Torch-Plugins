using SKO.Torch.Shared.Utils;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace SKO.GridPCULimiter.Commands
{
    [Category("gridpculimiter")]
    public class AdminCommands : CommandModule
    {
        [Command("reload", ".")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            SKOGridPCULimiterPlugin.Config =
                ConfigUtils.Load<GridPCULimiterConfig>(SKOGridPCULimiterPlugin.Instance, "SKOGridPCULimiter.cfg");
            Context.Respond("Configuration reloaded.");
        }

        [Command("config", ".")]
        [Permission(MyPromoteLevel.Admin)]
        public void Config()
        {
            var sb = new StringBuilder();

            sb.AppendLine("SKO-GridPCULimiter Config:");
            sb.AppendLine($"> Enabled: {SKOGridPCULimiterPlugin.Config.Enabled}");
            sb.AppendLine($"> MaxGridPCU: {SKOGridPCULimiterPlugin.Config.MaxGridPCU}");
            sb.AppendLine($"> AllowProjection: {SKOGridPCULimiterPlugin.Config.AllowProjection}");
            sb.AppendLine($"> AllowMerge: {SKOGridPCULimiterPlugin.Config.AllowMerge}");
            sb.AppendLine($"> IncludeConnectedGridsPCU: {SKOGridPCULimiterPlugin.Config.IncludeConnectedGridsPCU}");
            sb.AppendLine($"> MaxNumberOfConnectedGrids: {SKOGridPCULimiterPlugin.Config.MaxNumberOfConnectedGrids}");
            sb.AppendLine($"> DamageConnectors: {SKOGridPCULimiterPlugin.Config.DamageConnectors}");
            sb.AppendLine($"> DisableWeldersWithinMeters: {SKOGridPCULimiterPlugin.Config.DisableWeldersWithinMeters}");

            Context.Respond(sb.ToString());
        }
    }
}