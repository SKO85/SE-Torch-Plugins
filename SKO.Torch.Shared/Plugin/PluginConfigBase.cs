using Newtonsoft.Json;
using Torch;

namespace SKO.Torch.Shared.Plugin
{
    public abstract class PluginConfigBase : ViewModel
    {
        public bool PluginEnabled { get; set; }

        public abstract void Validate();

        public string GetConfigJSON()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}