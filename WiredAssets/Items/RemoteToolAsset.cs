using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class RemoteToolAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.RemoteTool;
        public RemoteToolAsset(Guid guid)
        {
            GUID = guid;
        }
    }
}
