using DemoPluginBridge;
using Microsoft.Extensions.Logging;
using URegistry.Core;


namespace DemoPluginReqTest
{
    [PluginIdentity(PluginType = typeof(DemoReqPluginImplementation), Name = "Demo_plugin_Req", IdPath = "The_Ultimate_Demo_Plugin", Authors = ["Margoovian", "Sparc"], MajorVersion = 2, MinorVersion = 5, PatchVersion = 1)]
    public sealed class DemoReqPluginImplementation : BasePlugin, IDemoPlugin
    {
        public void OnDeinitialized()
        {
            //throw new NotImplementedException();
        }

        public void OnInitialized()
        {
            Log(LogLevel.Information, "Internal Initialize");
            //throw new NotImplementedException();
        }

        public bool OnMount()
        {
            Requires("The_Ultimate_Demo_Plugin.Demo_plugin");
            return true;
        }

        public bool OnUnmount()
        {
            return true;
        }
    }
}
