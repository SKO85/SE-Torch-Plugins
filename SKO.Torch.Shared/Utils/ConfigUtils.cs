using System;
using System.IO;
using System.Xml.Serialization;
using Torch;

namespace SKO.Torch.Shared.Utils
{
    public static class ConfigUtils
    {
        /// <summary>
        ///     Generic Configuration/Data Load function to retreive data from specified file in the specified type.
        /// </summary>
        /// <typeparam name="T">The type of your configuration or data object</typeparam>
        /// <param name="plugin">Instance to the plugin</param>
        /// <param name="fileName">Name of the file to use</param>
        /// <returns></returns>
        public static T Load<T>(TorchPluginBase plugin, string fileName) where T : new()
        {
            var filePath = Path.Combine(plugin.StoragePath, fileName);
            var config = new T();

            try
            {
                if (File.Exists(filePath))
                    using (var streamReader = new StreamReader(filePath))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(T));
                        config = (T)xmlSerializer.Deserialize(streamReader);
                    }
                else
                    Save(plugin, config, fileName);
            }
            catch
            {
                Save(plugin, config, fileName);
            }

            return config;
        }

        /// <summary>
        ///     Saves the specified data instance to a file.
        /// </summary>
        /// <typeparam name="T">The type of the data to save.</typeparam>
        /// <param name="plugin">Instance reference to your plugin</param>
        /// <param name="data">The actual data instance</param>
        /// <param name="fileName">Name of the file to store to</param>
        /// <returns></returns>
        public static bool Save<T>(TorchPluginBase plugin, T data, string fileName) where T : new()
        {
            try
            {
                var filePath = Path.Combine(plugin.StoragePath, fileName);
                using (var streamWriter = new StreamWriter(filePath))
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    xmlSerializer.Serialize(streamWriter, data);
                }

                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }
    }
}