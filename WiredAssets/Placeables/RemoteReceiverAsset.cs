using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class RemoteReceiverAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.RemoteReceiver;
        public RemoteReceiverAsset(Guid guid)
        {
            GUID = guid;
        }
    }
}
