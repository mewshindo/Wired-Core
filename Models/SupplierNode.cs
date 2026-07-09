using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Services;
using Wired.WiredAssets;
using Wired.WiredInteractables;

namespace Wired.Models
{
    public class SupplierNode : MonoBehaviour, IElectricNode
    {
        public uint InstanceID { get; private set; }
        public bool IsPowered { get; private set; }
        public IWiredAsset Asset { get; set; }
        public float Consumption { get; } = 0;
        public bool AllowPowerThrough { get; } = true;
        public float Supply { get; set; }
        public Transform WireConnectPoint { get; set; }
        public BarricadeDrop barricade { get; set; }

        private Interactable _interactable;
        public void SetPowered(bool powered)
        {
            IsPowered = powered;

            if (this.Asset is BatteryAsset) return;

            if(_interactable is InteractableSpot spot)
            {
                if(spot.isPowered != powered)
                    BarricadeManager.ServerSetSpotPowered(spot, powered);
            }
        }
        public void Uninitialize()
        {
            Destroy(this);
        }
        private void Awake()
        {
            InstanceID = BarricadeManager.FindBarricadeByRootTransform(this.transform).instanceID;

            if (this.TryGetComponent(out InteractableGenerator gen))
            {
                _interactable = gen;
                IsPowered = gen.fuel > 0 && gen.isPowered;
            }
            if (this.TryGetComponent(out InteractableSpot spot))
            {
                _interactable = spot;
            }

            var p = transform.Find("WireConnectPoint");
            if (p != null) WireConnectPoint = p;
            else WireConnectPoint = this.transform;
        }
    }
}
