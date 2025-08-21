using URegistry.Core.Communication;
using URegistry.Interfaces;

namespace DemoPluginBridge
{
    public interface IDemoPlugin : IPlugin
    {
        public CallEventWrapper<IDemoPlugin> HelloWorldHook { get; set; }
    }
}
