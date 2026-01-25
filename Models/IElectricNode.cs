namespace Wired.Models
{
    public interface IElectricNode
    {
        uint InstanceID { get; }
        bool IsPowered { get; }
        float Consumption { get; }
        bool AllowPowerThrough { get; }
        void SetPowered(bool powered);
    }
}