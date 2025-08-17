using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using URegistry.Interfaces;

namespace URegistry.Core
{
    public abstract class BasePlugin
    {
        protected void Requires<T>() where T : BasePlugin, IPlugin
        {
            PluginIdentityAttribute? pluginIdentityAttribute = typeof(T).GetCustomAttribute(typeof(PluginIdentityAttribute)) as PluginIdentityAttribute;
            if (pluginIdentityAttribute is not null)
            {
                BaseRegistry.AddPluginDependancy(pluginIdentityAttribute.Id, this);
            }
            else
            {
                Log(LogLevel.Warning, "You can only require a type that has the attribute \"PluginIdentity\" on it using this overload!");
            }
        }

        protected void Requires(string Id)
        {
            BaseRegistry.AddPluginDependancy(Id.ToLower().Replace(' ', '_'), this);
        }

        /// <summary>
        /// Logs a message through the registries logger is instantiated
        /// </summary>
        public void Log(LogLevel logLevel, string message, object?[]? args = null, [CallerMemberName] string memberName = "")
        {
            if(args is not null)
            {
                BaseRegistry.Log(logLevel, message, args, memberName);
            }
        }

        internal void OnRequiredPluginMounted(string pluginId)
        {
            Log(LogLevel.Information, "Triggered");
        }   


    }
}
