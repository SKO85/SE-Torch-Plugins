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
            var config = ConfigUtils.Load<GridPCULimiterConfig>(SKOGridPCULimiterPlugin.Instance, "SKOGridPCULimiter.cfg");

            // Use reflection to set the private property
            typeof(SKOGridPCULimiterPlugin).GetProperty("Config").SetValue(SKOGridPCULimiterPlugin.Instance, config);
            Context.Respond("Configuration reloaded.");
        }

        [Command("config", ".")]
        [Permission(MyPromoteLevel.Admin)]
        public void Config()
        {
            var sb = new StringBuilder();

            sb.AppendLine("SKO-GridPCULimiter Config:");
            sb.AppendLine($"> Enabled: {SKOGridPCULimiterPlugin.Instance.Config.Enabled}");
            sb.AppendLine($"> MaxGridPCU: {SKOGridPCULimiterPlugin.Instance.Config.MaxGridPCU}");
            sb.AppendLine($"> AllowProjection: {SKOGridPCULimiterPlugin.Instance.Config.AllowProjection}");
            sb.AppendLine($"> AllowMerge: {SKOGridPCULimiterPlugin.Instance.Config.AllowMerge}");
            sb.AppendLine($"> IncludeConnectedGridsPCU: {SKOGridPCULimiterPlugin.Instance.Config.IncludeConnectedGridsPCU}");
            sb.AppendLine($"> MaxNumberOfConnectedGrids: {SKOGridPCULimiterPlugin.Instance.Config.MaxNumberOfConnectedGrids}");
            sb.AppendLine($"> DamageConnectors: {SKOGridPCULimiterPlugin.Instance.Config.DamageConnectors}");
            sb.AppendLine($"> DisableWeldersWithinMeters: {SKOGridPCULimiterPlugin.Instance.Config.DisableWeldersWithinMeters}");

            Context.Respond(sb.ToString());
        }
    }
}