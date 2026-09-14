using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Rocket.API;

namespace Wired.Utilities;
public static class ConfigManager
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static T Load<T>(string filePath) where T : new()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var defaults = new T();
                TryLoadDefaults(defaults);
                return defaults;
            }

            string yaml = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(yaml))
            {
                var defaults = new T();
                TryLoadDefaults(defaults);
                return defaults;
            }

            var loaded = Deserializer.Deserialize<T>(yaml);

            if (loaded == null)
            {
                loaded = new T();
                TryLoadDefaults(loaded);
            }

            return loaded;
        }
        catch (Exception ex)
        {
            WiredLogger.Error($"Failed to load configuration file '{filePath}': {ex.Message}");

            var fallback = new T();
            TryLoadDefaults(fallback);
            return fallback;
        }
    }
    private static void TryLoadDefaults(object config)
    {
        if (config is IDefaultable defaultable)
            defaultable.LoadDefaults();
    }
}