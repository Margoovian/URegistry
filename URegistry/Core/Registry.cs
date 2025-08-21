using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Principal;
using URegistry.Core.Attributes;
using URegistry.Interfaces;

namespace URegistry.Core
{

    internal struct RegisteredPlugin<T>
    {
        public T Value { get; init; }
        public PluginIdentityAttribute Identity { get; init; }
        public bool IsReady { get; set; }

    }

    public sealed class PluginRegistry<T> : BaseRegistry, IDisposable where T : class, IPlugin
    {
        /// <summary>
        /// All successfully mounted plugins
        /// </summary>
        private List<RegisteredPlugin<T>> MountedPlugins { get; set; } = new List<RegisteredPlugin<T>>(0);

        /// <summary>
        /// All additional assemblies that have been loaded by the registry.
        /// </summary>
        private List<WeakReference<PluginAssemblyLoadContext>> LoadedAssemblies { get; set; } = new List<WeakReference<PluginAssemblyLoadContext>>(0);

        /// <summary>
        /// Graph of all plugins and their dependencies.
        /// </summary>
        private DependencyGraph DependencyGraph { get; set; } = new DependencyGraph();

        /// <summary>
        /// List of all plugin identities that have been loaded by the registry, this is only here to avoid calling GetAllPluginIdentityAttributes() multiple times, which would be expensive.
        /// </summary>
        IEnumerable<PluginIdentityAttribute> PluginIdentities;

        /// <summary>
        /// Called when a plugin is successfully mounted, this event is where you should setup your EventWrappers.
        /// </summary>
        public event Action<T> PluginMounted;

        /// <summary>
        /// Called when a plugin is successfully unmounted.
        /// </summary>
        public event Action<T> PluginUnmounted;

        /// <summary>
        /// Called when a plugin is successfully initialized.
        /// </summary>
        public event Action<T> PluginInitialized;

        /// <summary>
        /// Called when a plugin is successfully deinitialized.
        /// </summary>
        public event Action<T> PluginDeinitialized;

        public PluginRegistry() { }

        /// <summary>
        /// Mounts a plugin by loading the assembly from the specified path and instantiating the plugin class.
        /// </summary>
        /// <param name="pluginPath"></param>
        /// <returns></returns>
        internal bool MountPlugin(string pluginId)
        {

            foreach (PluginIdentityAttribute identity in PluginIdentities)
            {

                if (identity.Id != pluginId) continue;

                T? pluginInstance = Activator.CreateInstance(identity.PluginType, nonPublic: true) as T;

                if (pluginInstance is null)
                {
                    RegistryLogger?.Log(LogLevel.Error, "Failed to instantiate plugin.\n\tPlugin Id: {0}", identity.Id);
                    return false;
                }

                if (!pluginInstance.OnMount())
                {
                    RegistryLogger?.Log(LogLevel.Error, "Failed to mount plugin.\n\tPlugin Name: {0}, Version: {1}, Id: {2}, Authors: {3}",
                        identity.Name, identity.Version, identity.Id, string.Join(", ", identity.Authors));
                    DependencyGraph.SetNodeState(identity, PluginState.Error);
                    return false;
                }

                RegisteredPlugin<T> mountedPlugin = new RegisteredPlugin<T>
                {
                    Value = pluginInstance,
                    Identity = identity,
                    IsReady = false
                };

                MountedPlugins.Add(mountedPlugin);

                RegistryLogger?.Log(LogLevel.Information, "Successfully mounted plugin.\n\tPlugin Name: {0}, Version: {1}, Id: {2}, Authors: {3}",
                    identity.Name, identity.Version, identity.Id, string.Join(", ", identity.Authors));

                DependencyGraph.SetNodeState(identity, PluginState.Mounted);

                PluginMounted?.Invoke(pluginInstance);

                return true;

            }

            RegistryLogger?.Log(LogLevel.Error, "Failed to mount plugin. Plugin Id not found: {0}", pluginId);
            return false;
        }

        internal void InitializePlugins()
        {
            for (int i = 0; i < MountedPlugins.Count; i++)
            {
                RegisteredPlugin<T> plugin = MountedPlugins[i];
                if (plugin.Value is null) continue;

                plugin.Value.OnInitialized();

                DependencyGraph.SetNodeState(plugin.Identity, PluginState.Initialized);
                plugin.IsReady = true;

                PluginInitialized?.Invoke(plugin.Value);

                RegistryLogger?.Log(LogLevel.Information, "Successfully initialized plugin.\n\tPlugin Name: {0}, Version: {1}, Id: {2}, Authors: {3}",
                    plugin.Identity.Name, plugin.Identity.Version, plugin.Identity.Id, string.Join(", ", plugin.Identity.Authors));
            }
        }

        /// <summary>
        /// Final step of the plugin loading process, this is where the plugins are verified and their state is set to ready or error, verification is done on the plugins side.
        /// </summary>
        internal void FinalizeLoad()
        {
            for (int i = 0; i < MountedPlugins.Count; i++)
            {
                RegisteredPlugin<T> plugin = MountedPlugins[i];
                if (plugin.Value is null) continue;

                if (plugin.Value.Verify())
                {
                    plugin.IsReady = true;
                    RegistryLogger?.Log(LogLevel.Information, "Plugin {0} is ready.", plugin.Identity.Name);
                }
                else
                {
                    plugin.IsReady = false;
                    RegistryLogger?.Log(LogLevel.Error, "Plugin {0} failed verification.", plugin.Identity.Name);
                }
            }
        }

        /// <summary>
        /// Loads assembly with logging
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns></returns>
        internal bool LoadAssmebly(string assemblyPath)
        {
            try
            {
                PluginAssemblyLoadContext loadContext = new PluginAssemblyLoadContext(assemblyPath);
                string fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                loadContext.LoadFromAssemblyName(new AssemblyName(fileName));
                LoadedAssemblies.Add(new WeakReference<PluginAssemblyLoadContext>(loadContext, trackResurrection: true));
                RegistryLogger?.Log(LogLevel.Information, "Successfully loaded assembly.\n\tAssembly: {0}", Path.GetFileName(assemblyPath));
            }
            catch (Exception ex)
            {
                RegistryLogger?.Log(LogLevel.Error, "Failed to load assembly.\n\tAssembly: {0}\n\tException: {1}", Path.GetFileName(assemblyPath), ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Constructs the dependency graph of all mounted plugins.
        /// </summary>
        internal void ConstructDepenencyGraph()
        {

            ReloadIdentities();

            foreach (PluginIdentityAttribute identity in PluginIdentities)
            {

                PluginRequiresAttribute? softRequires = GetPluginSoftRequires(identity.PluginType);

                DependencyGraph.Add(identity, softRequires);
                
            }
        }

        /// <summary>
        /// Load all plugins in the specified folder.
        /// </summary>
        /// <param name="pluginFolderPath"></param>
        /// <returns></returns>
        public bool LoadPluginFolder(string pluginFolderPath)
        {
            foreach (string plugin in Directory.GetFiles(pluginFolderPath, "*.dll")) // Only load dlls because they are the "plugins"
            {
                LoadAssmebly(plugin);
            }

            ConstructDepenencyGraph();

            UpdateLoadedAssemblyState();

            // If the dependency graph is ready, mount all plugins. otherwise, set the state of all plugins that are not ready to error and mount the rest.
            if (DependencyGraph.IsReady())
            {
                MountPlugins();
            }
            else
            {
                // Set plugin states to error on all plugins that are not ready.
                foreach (var node in DependencyGraph)
                {
                    if (node.State != PluginState.Loaded)
                    {
                        node.State = PluginState.Error;
                        RegistryLogger?.Log(LogLevel.Error, "Plugin {0} does not have the required dependencies loaded. It will not be loaded", node.Name);
                    }
                }

                MountPlugins();

            }

            RegistryLogger?.Log(LogLevel.Information, "Mounted {0} plugins.", DependencyGraph.Count());

            InitializePlugins();

            FinalizeLoad();

            return true;
        }

        /// <summary>
        /// Mounts all plugins that are in the dependency graph that are ready. 
        /// </summary>
        private void MountPlugins()
        {
            foreach (DependencyNode plugin in DependencyGraph)
            {

                // TODO: turn this into a bitmask or something similar to reduce the number of checks
                if (plugin.State == PluginState.Unknown || plugin.State == PluginState.Unloaded || plugin.State == PluginState.Shutdown || plugin.State == PluginState.Error) continue;

                MountPlugin(plugin.Name);

            }
        }

        /// <summary>
        /// Updates the state of all loaded plugins in dependency graph to loaded (if loaded).
        /// </summary>
        private void UpdateLoadedAssemblyState()
        {
            foreach (var assemblyContext in LoadedAssemblies)
            {
                assemblyContext.TryGetTarget(out PluginAssemblyLoadContext? assemblyContextRef);
                assemblyContextRef?.Assemblies
                    .SelectMany(GetPluginIdentityAttributesInAssembly)
                    .ToList()
                    .ForEach(identity =>
                    {
                        DependencyGraph.SetNodeState(identity, PluginState.Loaded);
                        RegistryLogger?.Log(LogLevel.Information, "Plugin assembly loaded.\n\tPlugin Name: {0}, Version: {1}, Id: {2}, Authors: {3}",
                            identity.Name, identity.Version, identity.Id, identity.Authors);
                    }
                );
            }
        }

        /// <summary>
        /// Gets the PluginIdentity attribute of a plugin type.
        /// </summary>
        /// <typeparam name="TPlugin"></typeparam>
        /// <returns></returns>
        private PluginIdentityAttribute? GetPluginIdentity<TPlugin>()
        {
            Type pluginType = typeof(TPlugin);
            return pluginType.GetCustomAttribute(typeof(PluginIdentityAttribute)) as PluginIdentityAttribute;
        }


        /// <summary>
        /// Gets all PluginIdentity attributes from all assemblies loaded by the registry.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<PluginIdentityAttribute> GetAllPluginIdentityAttributes()
        {

            List<PluginIdentityAttribute> pluginIdentities = new List<PluginIdentityAttribute>(0);
            foreach (var weakRef in LoadedAssemblies)
            {
                if (!weakRef.TryGetTarget(out PluginAssemblyLoadContext? assemblyContext)) continue;
                foreach (var assembly in assemblyContext.Assemblies)
                {
                    if (assembly is null) continue;
                    pluginIdentities.AddRange(GetPluginIdentityAttributesInAssembly(assembly));
                }

            }

            return pluginIdentities;
        }

        /// <summary>
        /// Gets the PluginIdentity attribute of a plugin type.
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        private PluginIdentityAttribute? GetPluginIdentity(Type pluginType)
        {
            return pluginType.GetCustomAttribute(typeof(PluginIdentityAttribute)) as PluginIdentityAttribute;
        }

        /// <summary>
        /// Gets the PluginSoftRequires attribute of a plugin type.
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        private PluginRequiresAttribute? GetPluginSoftRequires(Type pluginType)
        {
            return pluginType.GetCustomAttribute(typeof(PluginRequiresAttribute)) as PluginRequiresAttribute;
        }

        /// <summary>
        /// Gets all PluginIdentity attributes in the specified assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private IEnumerable<PluginIdentityAttribute> GetPluginIdentityAttributesInAssembly(Assembly assembly)
        {
            return assembly.GetTypes()
                .Select(type => type.GetCustomAttribute(typeof(PluginIdentityAttribute)))
                .Where(attr => attr is not null)
                .Cast<PluginIdentityAttribute>();
        }

        private bool HasAnyPluginIdentityAttributesInAssembly(Assembly assembly)
        {
            return assembly.GetTypes()
                .Any(type => type.GetCustomAttribute(typeof(PluginIdentityAttribute)) is not null);
        }

        public void Dispose()
        {
            MountedPlugins.Clear();
            foreach (var assemblyContextRef in LoadedAssemblies)
            {
                try
                {
                    // There is some jank and things that could be improved here, but this is a simple implementation that works for now.
                    PluginAssemblyLoadContext? assemblyContext;
                    if (!assemblyContextRef.TryGetTarget(out assemblyContext))
                    {
                        RegistryLogger?.Log(LogLevel.Warning, "Failed to get target from weak reference, skipping unload.");
                        continue;
                    }

                    assemblyContext?.Unload();
                    RegistryLogger?.Log(LogLevel.Information, "Successfully unloaded assembly: {0}", assemblyContext?.Name);
                }
                catch (Exception ex)
                {
                    RegistryLogger?.Log(LogLevel.Error, "Failed to unload assembly.\n\tException: {1}", ex.Message);
                }
            }
            LoadedAssemblies.Clear();
        }

        private void ReloadIdentities()
        {
            PluginIdentities = GetAllPluginIdentityAttributes();
            RegistryLogger?.Log(LogLevel.Information, "Reloaded plugin identities. Count: {0}", PluginIdentities.Count());

        }
    }
}
