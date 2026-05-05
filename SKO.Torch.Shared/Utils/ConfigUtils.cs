using NLog;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Xml.Serialization;
using Torch;

namespace SKO.Torch.Shared.Utils
{
    /// <summary>
    ///     JSON configuration persistence (Newtonsoft.Json). Falls back to XmlSerializer once for legacy
    ///     files written by earlier versions of the plugin suite — those are migrated to JSON on the
    ///     first load and the original XML is preserved as <c>&lt;file&gt;.xml-backup</c>.
    /// </summary>
    public static class ConfigUtils
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        ///     Generic Configuration/Data Load function to retrieve data from the specified file in the specified type.
        ///     Reads JSON. If an existing file is in the legacy XML format, it is migrated to JSON automatically.
        /// </summary>
        public static T Load<T>(TorchPluginBase plugin, string fileName) where T : class, new()
        {
            var filePath = Path.Combine(plugin.StoragePath, fileName);

            if (!File.Exists(filePath))
            {
                var defaults = new T();
                Save(plugin, defaults, fileName);
                return defaults;
            }

            string text;
            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"Failed to read config '{fileName}' for plugin '{plugin.GetType().Name}'. Using defaults.");
                return new T();
            }

            // JSON files start with '{' or '[' — XML files start with '<'.
            var trimmed = text.TrimStart();
            var looksLikeJson = trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '[');

            if (looksLikeJson)
            {
                try
                {
                    var loaded = JsonConvert.DeserializeObject<T>(text, JsonSettings);
                    return loaded ?? new T();
                }
                catch (Exception ex)
                {
                    return MoveCorruptAside<T>(plugin, filePath, fileName, ex);
                }
            }

            // Legacy XML — migrate to JSON.
            T migrated;
            try
            {
                using (var reader = new StringReader(text))
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    migrated = (T)xmlSerializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                return MoveCorruptAside<T>(plugin, filePath, fileName, ex);
            }

            // Preserve the XML before overwriting with JSON.
            var backup = filePath + ".xml-backup";
            try
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(filePath, backup);
                Log.Info(
                    $"Migrated config '{fileName}' for plugin '{plugin.GetType().Name}' from XML to JSON. " +
                    $"Original XML preserved as '{Path.GetFileName(backup)}'.");
            }
            catch (Exception ex)
            {
                Log.Warn(ex,
                    $"Couldn't preserve XML backup for '{fileName}'; proceeding to overwrite with JSON.");
            }

            Save(plugin, migrated, fileName);
            return migrated;
        }

        /// <summary>
        ///     Saves the specified data instance to a file as JSON.
        /// </summary>
        public static bool Save<T>(TorchPluginBase plugin, T data, string fileName) where T : class, new()
        {
            var filePath = Path.Combine(plugin.StoragePath, fileName);
            try
            {
                var json = JsonConvert.SerializeObject(data, JsonSettings);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"Failed to save config '{fileName}' for plugin '{plugin.GetType().Name}'.");
                return false;
            }
        }

        private static T MoveCorruptAside<T>(TorchPluginBase plugin, string filePath, string fileName, Exception ex)
            where T : class, new()
        {
            var backup = filePath + ".broken-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try
            {
                File.Move(filePath, backup);
                Log.Error(ex,
                    $"Failed to load config '{fileName}' for plugin '{plugin.GetType().Name}'. " +
                    $"Moved corrupt file to '{Path.GetFileName(backup)}'. Using defaults.");
            }
            catch (Exception moveEx)
            {
                Log.Error(ex,
                    $"Failed to load config '{fileName}' for plugin '{plugin.GetType().Name}'. " +
                    $"Also failed to move corrupt file aside ({moveEx.Message}). Using defaults.");
            }

            return new T();
        }
    }
}
