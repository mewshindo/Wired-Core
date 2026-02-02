using SDG.Unturned;
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
        void SetPowered(bool powered);
    }
}