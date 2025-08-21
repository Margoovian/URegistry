using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using URegistry.Core.Attributes;

namespace URegistry.Core
{

    public abstract class BaseRegistry
    {
        protected static ILogger? RegistryLogger;

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
                builder.SetMinimumLevel(LogLevel.Debug);
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

    }
}
