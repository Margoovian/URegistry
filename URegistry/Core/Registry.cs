using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Security.Principal;
using URegistry.Interfaces;

namespace URegistry.Core
{
    public sealed class PluginRegistry<T> : BaseRegistry, IDisposable where T : IPlugin
    {
        /// <summary>
        /// All successfully mounted plugins
        /// </summary>
        public List<T> MountedPlugins { get; private set; } = new List<T>(0);

        /// <summary>
        /// Called when a plugin is successfully mounted and initialized with all Dependencies loaded.
        /// </summary>
        public event Action<T>? PluginReady;

        public PluginRegistry() { }

        /// <summary>
        /// Mounts a plugin by loading the assembly from the specified path and instantiating the plugin class.
        /// </summary>
        /// <param name="pluginPath"></param>
        /// <returns></returns>
        internal bool MountPlugin(string pluginPath)
        {
            Assembly? pluginAssembly = Assembly.LoadFile(pluginPath);
            Type[] types = pluginAssembly.GetTypes();

            // TODO: Log something here?

            foreach (Type plugin in types)
            {
                Attribute[] attributes = System.Attribute.GetCustomAttributes(plugin);
                IEnumerable<Attribute> identities = attributes.Where((attr) => { return attr is PluginIdentityAttribute; });

                if(identities.Count() <= 0)
                {
                    RegistryLogger?.Log(LogLevel.Warning, "Plugin does not have any plugin classes that have the plugin identity attribute! Please create a class that inherrits from both BasePlugin and IPlugin with the attribute PluginIdentity attacted.");
                    return false;
                }

                foreach (Attribute rawIdentity in identities)
                {
                    PluginIdentityAttribute identity = (PluginIdentityAttribute)rawIdentity;

                    if (plugin.IsAssignableTo(typeof(T)))
                    {

                        RegistryLogger?.Log(LogLevel.Information, "Mounting plugin class: {0}", identity.Name);
                        bool success = InstantiatePlugin(plugin, identity);
                        if (success)
                        {
                            RegistryLogger?.Log(LogLevel.Information, "Successfully mounted plugin class.\n\tPlugin class: {0}", identity.Name);
                            InternalOnMounted(identity);
                        }
                        else
                        {
                            RegistryLogger?.Log(LogLevel.Warning, "Unsuccessfully mounted.\n\tPlugin class: {0}", identity.Name);
                        }
                    }
                    else
                    {
                        RegistryLogger?.Log(LogLevel.Error, "Plugin identifier found attached to inherrited class not stemming from IPlugin, Please only use PluginIdentifier on classes that inherrit from IPlugin!\n\tPlugin class: {0}", identity.Name);
                        return false;
                    }

                }
            }
            return true;
        }

        /// <summary>
        /// Mounts all plugins in the specified folder.
        /// </summary>
        /// <param name="pluginFolderPath"></param>
        /// <returns></returns>
        public bool MountPluginFolder(string pluginFolderPath)
        {
            foreach (string plugin in Directory.GetFiles(pluginFolderPath, "*.dll")) // Only load dlls because they are the "plugins"
            {
                RegistryLogger?.Log(LogLevel.Information, "Mounting plugin: {0}", Path.GetFileName(plugin));
                bool success = MountPlugin(plugin);
                if (success)
                {
                    foreach(T pluginInstance in MountedPlugins)
                    {
                        PluginReady?.Invoke(pluginInstance);
                    }        
                }
                else
                {
                    RegistryLogger?.Log(LogLevel.Error, "Something went wrong when mounting plugin: {0}", Path.GetFileName(plugin));
                }
            }

            FinalizeMountingProcess();
            return true;
        }

        /// <summary>
        /// "Mounts" a plugin by instantiating it and calling the OnMount method aswell as Initializing it.
        /// </summary>
        /// <param name="pluginType"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private bool InstantiatePlugin(Type? pluginType, PluginIdentityAttribute identity)
        {
            if (pluginType is not null)
            {
                T? instantiatedPlugin = (T?)Activator.CreateInstance(pluginType);
                if (instantiatedPlugin is null)
                {
                    RegistryLogger?.Log(LogLevel.Warning, "Failed to instantiate.\n\tPlugin class: {0}", identity.Name);
                    return false;
                }

                RegistryLogger?.Log(LogLevel.Information, "Initializing plugin class: {0}", identity.Name);

                bool success = instantiatedPlugin.OnMount();

                if (success)
                {

                    instantiatedPlugin.OnInitialized();
                    RegistryLogger?.Log(LogLevel.Information, "Successfully initialized plugin class.\n\tVersion: {0}, Id: {0}, Plugin Name: {0}, Authors: {0}", identity.Version, identity.Id, identity.Name, identity.Authors);

                    MountedPlugins.Add(instantiatedPlugin);
                }
                else
                {
                    RegistryLogger?.Log(LogLevel.Warning, "Something failed inside the plugins mounting process.\n\t Plugin class: {0}", identity.Name);
                }
                return success;
            }
            return false;
        }

        /// <summary>
        /// Checks if a plugin is mounted by checking if the plugin type has the PluginIdentity attribute and then checking if the Id matches.
        /// </summary>
        /// <typeparam name="TPlugin"></typeparam>
        /// <returns></returns>
        internal override bool IsMounted<TPlugin>()
        {
            PluginIdentityAttribute? identity = GetPluginIdentity<TPlugin>();

            if(identity is not null)
            {
                return IsMounted(identity.Id);
            }

            return false;
        }

        /// <summary>
        /// Check if a plugin is mounted by checking if the Id matches the Id of any mounted plugin's PluginIdentity attribute.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        internal override bool IsMounted(string Id)
        {
            foreach(T plugin in MountedPlugins)
            {
                PluginIdentityAttribute? identity = GetPluginIdentity(plugin.GetType());
                if (identity is not null && identity.Id == Id)
                {
                    return true;
                }
            }
            return false;
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
        /// Gets the PluginIdentity attribute of a plugin type.
        /// </summary>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        private PluginIdentityAttribute? GetPluginIdentity(Type pluginType)
        {
            return pluginType.GetCustomAttribute(typeof(PluginIdentityAttribute)) as PluginIdentityAttribute;
        }

        /// <summary>
        /// Gets all PluginIdentity attributes in the specified assembly.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private IEnumerable<PluginIdentityAttribute> GetPluginIdenityAttributesInAssembly(Assembly assembly)
        {
            return assembly.GetTypes()
                .Select(type => type.GetCustomAttribute(typeof(PluginIdentityAttribute)))
                .Where(attr => attr is not null)
                .Cast<PluginIdentityAttribute>();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }


        /// <summary>
        /// Gets whether a plugin is mounted by checking if the plugin's hash code matches any of the mounted plugins' hash codes.
        /// </summary>
        /// <param name="basePlugin"></param>
        /// <returns></returns>
        internal override bool IsMounted(in BasePlugin basePlugin)
        {
            foreach(T plugin in MountedPlugins)
            {
                if (plugin.GetType() == basePlugin.GetType()) // If same type this should work
                {
                    return true;
                }
            }
            return false;
        }
    }
}
