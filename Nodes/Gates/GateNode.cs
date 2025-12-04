
namespace Wired.Nodes
{
    public class GateNode : Node
    {
        public bool IsOpen { get; private set; } = false;
        public bool SwitchableByPlayer { get; set; } = false;

        public void Toggle(bool state)
        {
            IsOpen = state;
            Plugin.Instance.UpdateAllNetworks();
        }

        public override void IncreaseVoltage(float amount)
        {
            if (!IsOpen) return;
        }

        public override void DecreaseVoltage(float amount)
        {
            if (!IsOpen) return;
        }
    }

}