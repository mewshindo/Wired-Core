using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Wired;

public class Config : IDefaultable
{
    [YamlComment("Output debug messages into console. Default: false")]
    public bool LogDebugMessages { get; set; }
    [YamlComment("Amount of delay frames between power recalculation. If your server experiences lag because of heavy Wired networks, try raising this value. Default: 1")]
    public ushort RecalculationRateLimit { get; set; }
    public WindConfig WindConfig { get; set; }

    public void LoadDefaults()
    {
        LogDebugMessages = false;
        RecalculationRateLimit = 1;

        WindConfig = new WindConfig
        {
            WindSpeedChangeRate = 0.02f,
            NoiseMapScale = 1f
        };
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class YamlCommentAttribute : Attribute
{
    public string Comment { get; }
    public YamlCommentAttribute(string comment) => Comment = comment; 
}

public class WindConfig
{
    [YamlComment("Rate at which the wind changes it's speed")]
    public float WindSpeedChangeRate { get; set; }

    [YamlComment("Wind noise map scale, the higher the value, the less volatile the wind becomes.")]
    public float NoiseMapScale { get; set; }
}
