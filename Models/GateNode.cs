using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wired.Services;
using Wired.WiredAssets;
using Wired.Wrappers;

namespace Wired.Models
{
    public class GateNode : MonoBehaviour, IElectricNode
    {
        public uint InstanceID { get; private set; }
        public bool IsPowered { get; set; }
        public IWiredAsset Asset { get; set; }
        public float Consumption { get; set; }
        public bool AllowPowerThrough { get; private set; }
        public bool SwitchableByPlayer { get; set; } = true;
        public Vector3 WireConnectPoint { get; set; }

        private InteractableSpot _spot;
        /// <summary>
        /// don't use this on a switchnode, Switch(bool state) exists for a reason !
        /// </summary>
        public void SetPowered(bool state) { }
        public void Switch(bool state)
        {
            AllowPowerThrough = state;
            if(_spot != null)
                BarricadeManager.ServerSetSpotPowered(_spot, state);

            Plugin.Instance.SendSwitchToggled(this, state);
        }
        private void Awake()
        {
            _spot = GetComponent<InteractableSpot>();
            AllowPowerThrough = _spot == null ? false : _spot.isPowered;
            InstanceID = BarricadeManager.FindBarricadeByRootTransform(this.transform).instanceID;

            var p = transform.Find("WireConnectPoint");
            if (p != null) WireConnectPoint = p.position;
        }
    }
}
