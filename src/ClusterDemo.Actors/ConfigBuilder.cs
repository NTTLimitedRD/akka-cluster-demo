using Akka.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClusterDemo.Actors
{
    /// <summary>
    ///		Builds an Akka <see cref="Config"/> from a <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    public sealed class ConfigBuilder
    {
        /// <summary>
        ///		Settings for serialising values into JSON (to be used as HOCON literals).
        /// </summary>
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter()
            },
            Formatting = Formatting.None
        };
        
        /// <summary>
        ///		Create a new <see cref="ConfigBuilder"/>.
        /// </summary>
        public ConfigBuilder()
        {
        }

        /// <summary>
        ///		The underlying configuration entries.
        /// </summary>
        public Dictionary<string, object> Entries { get; } = new Dictionary<string, object>();

        /// <summary>
        ///		Build an Akka <see cref="Config"/> from the underlying configuration entries.
        /// </summary>
        /// <returns>
        ///		The <see cref="Config"/>.
        /// </returns>
        public Config Build()
        {
            return ConfigurationFactory.ParseString(
                hocon: BuildHocon()
            );
        }

        /// <summary>
        ///		Build a HOCON string from the underlying configuration entries.
        /// </summary>
        /// <returns>
        ///		The HOCON string.
        /// </returns>
        public string BuildHocon()
        {
            StringBuilder hoconBuilder = new StringBuilder();
            foreach (string key in Entries.Keys)
            {
                string value = FormatHoconLiteral(Entries[key]);

                hoconBuilder.AppendFormat("{0} = {1}\n", key, value);
            }

            return hoconBuilder.ToString();
        }

        /// <summary>
        ///		Format the specified value as a HOCON literal.
        /// </summary>
        /// <param name="value">
        ///		The value to format.
        /// </param>
        /// <returns>
        ///		The value, as a string.
        /// </returns>
        static string FormatHoconLiteral(object value)
        {
            if (value == null)
                return String.Empty;

            return JsonConvert.SerializeObject(value, SerializerSettings);
        }
    }
}
