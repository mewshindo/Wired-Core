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
    public bool LogDebugMessages { get; set; }
    public ushort RecalculationRateLimit { get; set; }
    public WindConfig WindConfig { get; set; }

    public void LoadDefaults()
    {
        LogDebugMessages = true;
        RecalculationRateLimit = 1;

        WindConfig = new WindConfig
        {
            WindSpeedChangeRate = 0.02f,
            NoiseMapScale = 0.005f
        };
    }
}

public class WindConfig
{
    public float WindSpeedChangeRate;

    public float NoiseMapScale;
}
