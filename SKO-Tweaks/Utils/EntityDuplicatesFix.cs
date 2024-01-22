using Sandbox.Game.Entities;
using System;
using System.Linq;

namespace SKO.Torch.Plugins.Tweaks.Utils
{
    public static class EntityDuplicatesFix
    {
        public static void Fix()
        {
            try
            {
                MyEntities.GetEntities().ToDictionary(c => c.EntityId, c => c);
            }
            catch (Exception)
            {
                // Ok, we found an issue with duplicate entityIds.
                var entities = MyEntities.GetEntities();
                var duplicates = entities.GroupBy(c => c.EntityId, (a, b) => b.ToList()).Where(c => c.Count > 1)
                    .ToDictionary(c => c.First().EntityId, c => c);

                foreach (var item in duplicates)
                {
                    SKOTweaksPlugin.Log.Warn($"Found {item.Value.Count} duplicates for entityId: {item.Key}:");

                    foreach (var entity in item.Value)
                    {
                        var ob = entity.GetObjectBuilder();
                        MyEntities.RemapObjectBuilder(ob);
                        entity.Init(ob);

                        SKOTweaksPlugin.Log.Warn(
                            $"-> Name: {entity.Name}, DisplayName: {entity.DisplayName}, Type: {entity.GetType().Name}, Old-ID: {item.Key}, New-ID: {entity.EntityId}");
                    }
                }
            }
        }
    }
}