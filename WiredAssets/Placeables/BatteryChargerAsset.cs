using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    internal class BatteryChargerAsset : IWiredAsset
    {
        public Guid GUID { get; set; }

        public WiredAssetType Type { get; } = WiredAssetType.Consumer;
        public float ChargePerHour { get; set; }

        public BatteryChargerAsset(Guid guid, float chargePerHour)
        {
            GUID = guid;
            ChargePerHour = chargePerHour;
        }
    }
}
