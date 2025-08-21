using DemoPluginBridge;
using Microsoft.Extensions.Logging;
using URegistry.Core;
using URegistry.Core.Attributes;
using URegistry.Core.Communication;


namespace DemoPluginReqTest
{
    [
        PluginIdentity(PluginType = typeof(DemoReqPluginImplementation), Name = "Demo_plugin_Req", IdPath = "The_Ultimate_Demo_Plugin", Authors = ["Margoovian", "Sparc"], MajorVersion = 2, MinorVersion = 5, PatchVersion = 1),
        PluginRequires(
            "The_Ultimate_Demo_Plugin.Demo_plugin"
        )
    ]
    public sealed class DemoReqPluginImplementation : BasePlugin, IDemoPlugin
    {
        public CallEventWrapper<IDemoPlugin> HelloWorldHook { get; set; }

        public void OnDeinitialized()
        {
            //throw new NotImplementedException();
        }

        public void OnInitialized()
        {
            Log(LogLevel.Information, "Plugin 2 Initialized");
            //throw new NotImplementedException();
        }

        public bool OnMount()
        {
            return true;
        }

        public bool OnUnmount()
        {
            return true;
        }
    }
}
