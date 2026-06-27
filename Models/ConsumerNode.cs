using SDG.Unturned;
using System.Linq;
using UnityEngine;
using Wired.WiredAssets;
using Wired.Wrappers;

namespace Wired.Models
{
    public class ConsumerNode : MonoBehaviour, IElectricNode
    {
        public uint InstanceID {  get; private set; }
        public IWiredAsset Asset { get; set; }
        public bool IsPowered { get; set; } = true;
        public float Consumption { get; set; }
        public bool AllowPowerThrough { get; set; } = true;
        public Vector3 WireConnectPoint { get; set; }

        private ConsumerInteractable Interactable;
        public void SetPowered(bool powered)
        {
            IsPowered = powered;
            Interactable.SetPowered(powered);
        }
        private void Awake()
        {
            InstanceID = BarricadeManager.FindBarricadeByRootTransform(this.transform).instanceID;
            Interactable = new ConsumerInteractable(this.transform);

            var p = transform.Find("WireConnectPoint");
            if (p != null) WireConnectPoint = p.position;
        }
    }
}
