using SKO.Torch.Shared.Utils;
using System;
using Torch;

namespace SKO.Torch.Shared.Plugin
{
    public abstract class PluginMainModule<TConfig, TTorchPluginBase> : PluginBaseModule<TTorchPluginBase>
        where TConfig : PluginConfigBase, new()
        where TTorchPluginBase : TorchPluginBase
    {
        public Action<TConfig> ConfigLoaded;

        protected PluginMainModule(TTorchPluginBase pluginInstance, string configFile = null) : base(pluginInstance)
        {
            ConfigFile = configFile;
        }

        public string ConfigFile { get; set; }
        public static TConfig Config { get; set; }

        public override void Init()
        {
            if (!IsInitialized)
            {
                // Initialize configuration file.
                InitConfig();

                base.Init();
            }
        }

        protected virtual void InitConfig()
        {
            LoadConfig();
            SaveConfig();
        }

        public void LoadConfig()
        {
            if (!string.IsNullOrEmpty(ConfigFile))
            {
                Config = ConfigUtils.Load<TConfig>(TorchPluginInstance, ConfigFile);
                Config?.Validate();

                // Call event.
                if (ConfigLoaded != null && Config != null) ConfigLoaded.Invoke(Config);
            }
        }

        public void SaveConfig()
        {
            if (!string.IsNullOrEmpty(ConfigFile) && Config != null)
                ConfigUtils.Save(TorchPluginInstance, Config, ConfigFile);
        }
    }
}