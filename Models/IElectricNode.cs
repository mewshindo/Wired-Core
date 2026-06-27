using SDG.Unturned;
using UnityEngine;
using Wired.WiredAssets;

namespace Wired.Models
{
    public interface IElectricNode
    {
        uint InstanceID { get; }
        IWiredAsset Asset { get; }
        bool IsPowered { get; }
        float Consumption { get; }
        bool AllowPowerThrough { get; }
        Vector3 WireConnectPoint { get; }
        void SetPowered(bool powered);
    }
}