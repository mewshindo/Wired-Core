using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Models;
using Wired.WiredAssets;

namespace Wired.WiredInteractables
{
    public class DaylightSensor : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }
        
        public bool IsOn { get; private set; }
        public bool IsObstructed { get; private set; }

        private DaylightSensorAsset _asset;
        private GateNode _gate;
        public void Start()
        {
            _gate = gameObject.GetComponent<GateNode>();
            _asset = (DaylightSensorAsset)_gate.Asset;

            Plugin.OnTimeOfDayUpdated += OnTimeOfDayUpdated;
        }

        private void OnTimeOfDayUpdated(uint timeOfDay, float timefraction)
        {
            Raycast raycast = new(null, 64);
            if(!raycast.GetPoint(transform.position, Vector3.up, out _))
            {
                IsObstructed = false;
                SetPowered(LightingManager.isDaytime == (_asset.Mode == DaylightSensorMode.Day));
            }
            else
            {
                IsObstructed = true;
                SetPowered(_asset.Mode != DaylightSensorMode.Day);
            }
        }

        public void SetPowered(bool state)
        {
            if(_gate.AllowPowerThrough != state)
                _gate.Switch(state);
        }

        public void Uninitialize()
        {
            
        }
    }
}
