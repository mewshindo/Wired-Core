using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wired.Services;
using Wired.Wrappers;

namespace Wired.Models
{
    public class SwitchNode : MonoBehaviour, IElectricNode
    {
        public uint InstanceID { get; private set; }
        public bool IsPowered { get; set; }
        public float Consumption { get; set; }
        public bool AllowPowerThrough { get; private set; }
        public void SetPowered(bool powered) { }
        public void Switch(bool state)
        {
            AllowPowerThrough = state;
            var spot = GetComponent<InteractableSpot>();
            BarricadeManager.ServerSetSpotPowered(spot, state);
            Plugin.Instance.SendSwitchToggled(this, state);
        }
        private void Awake()
        {
            AllowPowerThrough = GetComponent<InteractableSpot>().isPowered;
            InstanceID = BarricadeManager.FindBarricadeByRootTransform(this.transform).instanceID;
        }
    }
}
