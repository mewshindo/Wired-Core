using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    internal class NetworkDataDisplayAsset : IWiredAsset
    {
        public Guid GUID { get; private set; }

        public WiredAssetType Type { get; } = WiredAssetType.Consumer;

        public float Consumption;
        public NetworkDataDisplayAsset(Guid guid)
        {
            GUID = guid;
        }
    }
}
