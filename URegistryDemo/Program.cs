using DemoPluginBridge;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using URegistry;
using URegistry.Core;

namespace URegistryDemo
{
    internal class Program
    {
        internal PluginRegistry<IDemoPlugin> PluginRegistry;
        public Program()
        {
            PluginRegistry = new PluginRegistry<IDemoPlugin>();
            PluginRegistry.InitializeLogger();
        }
        public void Run()
        {
            PluginRegistry.MountPluginFolder(@"G:\Projects\C#\URegistry\bin\"); // Hard coded path for demo
        }
    }
}
