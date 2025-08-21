using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace URegistry.Core
{
    internal class PluginAssemblyLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public PluginAssemblyLoadContext(string mainAssemblyToLoadPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName name)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(name);
            BaseRegistry.Log(LogLevel.Debug, "Resolving assembly {0} to path {1}", [name.FullName, assemblyPath]);
            if (assemblyPath != null)
            {
                BaseRegistry.Log(LogLevel.Debug, "Loading assembly {0} from path {1}", [name.FullName, assemblyPath]);
                return LoadFromAssemblyPath(assemblyPath);
            }
            BaseRegistry.Log(LogLevel.Warning, "Could not resolve assembly {0} to a path", [name.FullName]);
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
