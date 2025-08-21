using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using URegistry.Core.Attributes;
using URegistry.Interfaces;

namespace URegistry.Core
{
    public abstract class BasePlugin
    {
        /// <summary>
        /// Logs a message through the registries logger is instantiated
        /// </summary>
        public void Log(LogLevel logLevel, string message, object?[]? args = null, [CallerMemberName] string memberName = "")
        {

            MethodBase? methodInfo = new StackTrace()?.GetFrame(1)?.GetMethod();
            string? className = methodInfo?.ReflectedType?.Name;

            if (args is not null)
            {
                BaseRegistry.Log( logLevel, $"{className} -> " + message, args, memberName);
            } else
            {
                BaseRegistry.Log(logLevel, $"{className} -> " + message, Array.Empty<object>(), memberName);
            }
        }

    }
}
