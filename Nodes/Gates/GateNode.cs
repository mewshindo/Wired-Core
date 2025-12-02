
namespace Wired.Nodes
{
    public class GateNode : Node
    {
        public bool IsOn { get; private set; } = false;
        public bool SwitchableByPlayer { get; set; } = false;

        public void Toggle(bool state)
        {
            IsOn = state;
            Plugin.Instance.UpdateAllNetworks();
        }

        public override void IncreaseVoltage(uint amount)
        {
            if (!IsOn) return;
        }

        public override void DecreaseVoltage(uint amount)
        {
            if (!IsOn) return;
        }
    }

}