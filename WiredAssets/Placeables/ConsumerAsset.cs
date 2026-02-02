using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class ConsumerAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.Consumer;
        public float Consumption { get; set; }
        public ConsumerAsset(Guid guid, float consumption)
        {
            GUID = guid;
            Consumption = consumption;
        }
    }
}
