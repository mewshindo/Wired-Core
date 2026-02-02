using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public class SupplierAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type { get; } = WiredAssetType.Supplier;

        public float Supply { get; set; }

        public SupplierAsset(Guid guid, float supply)
        {
            GUID = guid;
            Supply = supply;
        }
    }
}
