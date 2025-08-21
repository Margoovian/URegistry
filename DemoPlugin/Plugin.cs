using DemoPluginBridge;
using Microsoft.Extensions.Logging;
using URegistry.Core;
using URegistry.Core.Attributes;
using URegistry.Core.Communication;

namespace DemoPlugin
{

    [
        PluginIdentity(
        PluginType = typeof(DemoPluginImplementation),
        Name = "Demo_plugin",
        IdPath = "The_Ultimate_Demo_Plugin",
        Authors = ["Margoovian", "Sparc"],
        MajorVersion = 2, MinorVersion = 5, PatchVersion = 1),
        PluginRequires(
            "The_Ultimate_Demo_Plugin.Demo_plugin_Req"
        )
    ]
    public sealed class DemoPluginImplementation : BasePlugin, IDemoPlugin
    {
        public CallEventWrapper<IDemoPlugin> HelloWorldHook { get; set; }

        public void OnDeinitialized()
        {
            //throw new NotImplementedException();
        }

        public void OnInitialized()
        {
            Log(LogLevel.Information, "Plugin 1 Initialized");

            HelloWorldHook.Call(this, null);

            //throw new NotImplementedException();
        }

        public bool OnMount()
        {
            return true;
            //throw new NotImplementedException();
        }

        public bool OnUnmount()
        {
            return true;
            //throw new NotImplementedException();
        }
    }

    


}
