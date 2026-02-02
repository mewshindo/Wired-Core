using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class RemoteTransmitterAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.RemoteTransmitter;
        public float Consumption { get; set; } = 5f;
        public float Range { get; set; } = 50f;
        public RemoteTransmitterAsset(Guid guid, float consumption, float range)
        {
            GUID = guid;
            Consumption = consumption;
            Range = range;
        }
    }
}
