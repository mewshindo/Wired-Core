using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.WiredAssets;

namespace Wired.Models
{
    public class SupplierNode : MonoBehaviour, IElectricNode
    {
        public uint InstanceID { get; private set; }
        public bool IsPowered { get; private set; } = true;
        public IWiredAsset Asset { get; set; }
        public float Consumption { get; } = 0;
        public bool AllowPowerThrough { get; } = true;
        public float Supply { get; set; } = 1500;
        private InteractableGenerator gen;
        public void SetPowered(bool powered)
        {
            IsPowered = powered;
        }
        private void Awake()
        {
            InstanceID = BarricadeManager.FindBarricadeByRootTransform(this.transform).instanceID;
            gen = this.GetComponent<InteractableGenerator>();
            IsPowered = gen.fuel > 0 && gen.isPowered;
        }
    }
}
