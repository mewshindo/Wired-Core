using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class EngineerGogglesAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.EngineerGoggles;
        public EngineerGogglesAsset(Guid guid)
        {
            GUID = guid;
        }
    }
}
