using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class SwitchAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.Switch;
        public bool SwitchableByPlayer { get; set; } = false;
        public SwitchAsset(Guid guid, bool switchable)
        {
            GUID = guid;
            SwitchableByPlayer = switchable;
        }
    }
}
