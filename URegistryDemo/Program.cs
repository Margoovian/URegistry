using DemoPluginBridge;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using URegistry;
using URegistry.Core;
using URegistry.Core.Communication;
using static System.Net.Mime.MediaTypeNames;

namespace URegistryDemo
{
    internal class Program
    {
        internal PluginRegistry<IDemoPlugin> PluginRegistry;
        public Program()
        {
            PluginRegistry = new PluginRegistry<IDemoPlugin>();
            PluginRegistry.InitializeLogger();

            PluginRegistry.PluginMounted += OnPluginMounted;
        }
        public void Run()
        {
            string loadPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName,@"bin\Plugins\Debug\net9.0\");
            PluginRegistry.LoadPluginFolder(loadPath); // Hard coded path for demo
        }


        public void OnPluginMounted(IDemoPlugin plugin)
        {
            plugin.HelloWorldHook = new CallEventWrapper<IDemoPlugin> { Func = HelloWorld };
        }

        private void HelloWorld(IDemoPlugin sender, EventArgs args)
        {
            BaseRegistry.Log(Microsoft.Extensions.Logging.LogLevel.Information,"Hello World from {0}", [sender.GetType().Name]);
        }

    }
}
