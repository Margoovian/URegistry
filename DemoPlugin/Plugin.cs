using DemoPluginBridge;
using Microsoft.Extensions.Logging;
using URegistry.Core;

namespace DemoPlugin
{

    [PluginIdentity(PluginType = typeof(DemoPluginImplementation), Name = "Demo_plugin", IdPath = "The_Ultimate_Demo_Plugin", Authors = ["Margoovian", "Sparc"], MajorVersion = 2, MinorVersion = 5, PatchVersion = 1)]
    public sealed class DemoPluginImplementation : BasePlugin, IDemoPlugin
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
            Requires("The_Ultimate_Demo_Plugin.Demo_plugin_Req");
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
