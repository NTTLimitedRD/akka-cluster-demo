using Akka.Actor;
using System;
using System.Collections.Generic;

using AkkaLogLevel = Akka.Event.LogLevel;

namespace ClusterDemo.Actors
{
    /// <summary>
    ///		Extension methods for <see cref="ConfigBuilder"/>.
    /// </summary>
    public static class ConfigBuilderExtensions
    {
        /// <summary>
        ///		Set the system log level.
        /// </summary>
        /// <param name="configBuilder">
        ///		The configuration builder.
        /// </param>
        /// <param name="level">
        ///		The target log level.
        /// </param>
        /// <returns>
        ///		The configuration builder (enables method chaining).
        /// </returns>
        public static ConfigBuilder SetLogLevel(this ConfigBuilder configBuilder, AkkaLogLevel level)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            string logLevelValue;
            switch (level)
            {
                case AkkaLogLevel.DebugLevel:
                {
                    logLevelValue = "debug";

                    break;
                }
                case AkkaLogLevel.InfoLevel:
                {
                    logLevelValue = "info";

                    break;
                }
                case AkkaLogLevel.WarningLevel:
                {
                    logLevelValue = "warning";

                    break;
                }
                case AkkaLogLevel.ErrorLevel:
                {
                    logLevelValue = "error";

                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid log level.");
                }
            }

            configBuilder.Entries["akka.loglevel"] = logLevelValue;

            return configBuilder;
        }

        /// <summary>
        ///		Add a logger to the configuration.
        /// </summary>
        /// <typeparam name="TLogger">
        ///		The type of logger to add.
        /// </typeparam>
        /// <param name="configBuilder">
        ///		The configuration builder.
        /// </param>
        /// <returns>
        ///		The configuration builder (enables method chaining).
        /// </returns>
        public static ConfigBuilder AddLogger<TLogger>(this ConfigBuilder configBuilder)
            where TLogger : ActorBase
        {
            return configBuilder.AddLogger(typeof(TLogger));
        }

        /// <summary>
        ///		Add a logger to the configuration.
        /// </summary>
        /// <param name="configBuilder">
        ///		The configuration builder.
        /// </param>
        /// <param name="loggerType">
        ///		The type of logger to add.
        /// </param>
        /// <returns>
        ///		The configuration builder (enables method chaining).
        /// </returns>
        public static ConfigBuilder AddLogger(this ConfigBuilder configBuilder, Type loggerType)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            if (loggerType == null)
                throw new ArgumentNullException(nameof(loggerType));

            object value;
            List<string> loggerTypes;
            if (!configBuilder.Entries.TryGetValue("akka.loggers", out value) || (loggerTypes = value as List<string>) == null)
                configBuilder.Entries["akka.loggers"] = loggerTypes = new List<string>();

            string loggerTypeName = $"{loggerType.FullName}, {loggerType.Assembly.GetName().Name}";
            if (!loggerTypes.Contains(loggerTypeName))
                loggerTypes.Add(loggerTypeName);

            return configBuilder;
        }

        public static ConfigBuilder SuppressJsonSerializerWarning(this ConfigBuilder configBuilder)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            configBuilder.Entries["akka.suppress-json-serializer-warning"] = "on";

            return configBuilder;
        }

        public static ConfigBuilder UseRemoting(this ConfigBuilder configBuilder, string hostName, int port)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            configBuilder.Entries["akka.remote.helios.tcp.hostname"] = hostName;
            configBuilder.Entries["akka.remote.helios.tcp.port"] = port;

            return configBuilder;
        }

        public static ConfigBuilder UseCluster(this ConfigBuilder configBuilder, params string[] seedNodes)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            if (seedNodes.Length > 0)
                configBuilder.Entries["akka.cluster.seed-nodes"] = new List<string>(seedNodes);

            return configBuilder.UseClusterActorRefProvider();
        }

        public static ConfigBuilder UseClusterActorRefProvider(this ConfigBuilder configBuilder)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));
            
            return configBuilder.UseActorRefProvider("Akka.Cluster.ClusterActorRefProvider, Akka.Cluster");
        }

        public static ConfigBuilder UseActorRefProvider<TProvider>(this ConfigBuilder configBuilder)
            where TProvider : IActorRefProvider
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            Type providerType = typeof(TProvider);

            return configBuilder.UseActorRefProvider(String.Format("{0}, {1}",
                providerType.FullName,
                providerType.Assembly.GetName().Name
            ));
        }

        public static ConfigBuilder UseActorRefProvider(this ConfigBuilder configBuilder, string providerType)
        {
            if (configBuilder == null)
                throw new ArgumentNullException(nameof(configBuilder));

            configBuilder.Entries["akka.actor.provider"] = providerType;

            return configBuilder;
        }
    }
}
