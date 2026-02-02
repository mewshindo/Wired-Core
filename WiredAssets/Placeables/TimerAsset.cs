using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class TimerAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.Timer;
        public float DelaySeconds { get; set; } = 1.0f;
        public TimerAsset(Guid guid, float delayseconds)
        {
            GUID = guid;
            DelaySeconds = delayseconds;
        }
    }
}
