using Torch;

namespace SKO.Torch.Shared.Plugin
{
    public abstract class PluginExtensionModule<TTorchPluginBase> : PluginBaseModule<TTorchPluginBase>
        where TTorchPluginBase : TorchPluginBase
    {
        protected PluginExtensionModule(TTorchPluginBase pluginInstance) : base(pluginInstance)
        {
        }
    }
}