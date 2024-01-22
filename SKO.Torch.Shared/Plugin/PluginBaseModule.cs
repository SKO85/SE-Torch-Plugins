using Torch;

namespace SKO.Torch.Shared.Plugin
{
    public abstract class PluginBaseModule<TTorchPluginBase> where TTorchPluginBase : TorchPluginBase
    {
        protected TTorchPluginBase TorchPluginInstance;

        protected PluginBaseModule(TTorchPluginBase pluginInstance)
        {
            TorchPluginInstance = pluginInstance;
        }

        protected bool IsRunning { get; set; }
        protected bool IsInitialized { get; set; }

        public virtual void Stop()
        {
            // Set to false.
            IsRunning = false;
        }

        public virtual void Init()
        {
            if (!IsInitialized)
            {
                // Initialize module.
                InitializeModule();

                // Set to true.
                IsInitialized = true;
            }
        }

        protected abstract void InitializeModule();
        public abstract void Start();
    }
}