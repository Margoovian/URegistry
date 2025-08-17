using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace URegistry.Core
{

    internal struct RequiredPlugin
    {
        public string Id { get; init; }
        public event Action<string> OnRequiredPluginMounted;
        public void PluginMounted(string pluginId)
        {

            OnRequiredPluginMounted?.Invoke(pluginId);

            if (OnRequiredPluginMounted is not null)
            {
                foreach (var subscription in OnRequiredPluginMounted.GetInvocationList().ToList())
                {
                    OnRequiredPluginMounted -= (Action<string>)subscription;
                }
            }

        }
    }

    public abstract class BaseRegistry
    {
        protected static ILogger? RegistryLogger;
        internal static Dictionary<RequiredPlugin, List<BasePlugin>> PluginsWithDependancies = new Dictionary<RequiredPlugin, List<BasePlugin>>(0);

        public void InitializeLogger(params ILoggerProvider[] providers)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "[ HH:mm:ss ]: ";
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            foreach (ILoggerProvider provider in providers)
            {
                loggerFactory.AddProvider(provider);
            }

            RegistryLogger = loggerFactory.CreateLogger("Plugin Registry");

            RegistryLogger.Log(LogLevel.Information, "Logger initialized");

        }

        public static ILogger GetRegistryLogger()
        {
            if (RegistryLogger is not null)
            {
                return RegistryLogger;
            }
            throw new Exception("Logger is not initialized yet!");
        }

        public static void Log(LogLevel logLevel, string? message, object?[] args, [CallerMemberName] string memberName = "")
        {
            using (RegistryLogger?.BeginScope("{0}:", memberName))
            {
                RegistryLogger?.Log(logLevel, message, args);
            }

        }

        internal static void AddPluginDependancy(string dependancyId, in BasePlugin pluginWithDependancy)
        {
            RequiredPlugin? requiredPlugin = null;
            if (DependancyIsAlreadyInDepList(dependancyId, ref requiredPlugin))
            {
                if (requiredPlugin is null)
                {
                    Log(LogLevel.Error, "RequiredPlugin is null, this should not happen!", Array.Empty<object>());
                    return;
                }

                RequiredPlugin requiredPluginNonNull = requiredPlugin.Value;
                var basePluginList = PluginsWithDependancies[requiredPluginNonNull];

                if (basePluginList.Count <= 0)
                {
                    requiredPluginNonNull.OnRequiredPluginMounted += pluginWithDependancy.OnRequiredPluginMounted;
                    basePluginList = new List<BasePlugin> { pluginWithDependancy };
                }
                else
                {
                    requiredPluginNonNull.OnRequiredPluginMounted += pluginWithDependancy.OnRequiredPluginMounted;
                    basePluginList.Add(pluginWithDependancy);
                }
            }
            else
            {

                RequiredPlugin dependancy = new RequiredPlugin() { Id = dependancyId };
                dependancy.OnRequiredPluginMounted += pluginWithDependancy.OnRequiredPluginMounted;

                PluginsWithDependancies.Add(dependancy, new List<BasePlugin> { pluginWithDependancy });
            }
        }
        protected bool FinalizeMountingProcess()
        {
            if(PluginsWithDependancies.Count > 0)
            {
                foreach(var pluginDepPair in PluginsWithDependancies)
                {
                    Log(LogLevel.Error, "Plugin dependency is not loaded!\n\t Dependency Id: {0}\n\t Dependants: {0} ", [pluginDepPair.Key.Id, pluginDepPair.Value]);
                }

                Log(LogLevel.Warning, "Some plugin(s) don't have depenencies loaded.", Array.Empty<object>());

                return false;

            }

            Log(LogLevel.Information, "All plugin(s) have been mounted with possible dependencies.", Array.Empty<object>());

            return true;

        }
        internal abstract bool IsMounted<T>();
        internal abstract bool IsMounted(in BasePlugin basePlugin);
        internal abstract bool IsMounted(string Id);
        internal void InternalOnMounted(PluginIdentityAttribute pluginIdentity)
        {
            RequiredPlugin? requiredPlugin = GetRequiredPluginFromList(pluginIdentity.Id);
            if (requiredPlugin is not null)
            {
                Log(LogLevel.Information, "Required plugin mounted: {0}", [pluginIdentity.Id]);
                requiredPlugin?.PluginMounted(pluginIdentity.Id);

                // Have to do this extra step because of cyclic dependencies
                foreach (var pluginDep in PluginsWithDependancies[requiredPlugin.Value])
                {
                    if(IsMounted(pluginDep))
                    {
                        Log(LogLevel.Error, "Cyclic plugin dependencies detected, make sure plugins do not depend on each other. This will be implemented in the future.\n\t Required Plugin Class: {0}\n\t Plugin Dependant: {0}", [pluginDep.ToString(), requiredPlugin?.Id]);
                    }
                }

                PluginsWithDependancies.Remove(requiredPlugin.Value);
            }
        }
        private static bool DependancyIsAlreadyInDepList(string dependancyId, ref RequiredPlugin? requiredPlugin)
        {
            requiredPlugin = GetRequiredPluginFromList(dependancyId);
            if (requiredPlugin is not null)
            {
                return true;
            }
            return false;
        }

        private static RequiredPlugin? GetRequiredPluginFromList(string Id)
        {
            var dependancyList = PluginsWithDependancies.Where(x => x.Key.Id == Id);
            if (dependancyList.Count() > 0)
            {
                return dependancyList.First().Key;
            }
            return null;
        }

    }
}
