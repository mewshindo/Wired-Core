using Rocket.Core.Assets;
using SDG.Unturned;
using System;
using UnityEngine;

namespace Wired.WiredInteractables
{
    public class BatteryCharger : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }
        private ItemJar battery;

        public float ChargeRateUnitsPerHour { get; set; } = 100;

        private float _chargeRate;
        private float _dueCharge;
        private float timeSinceLastUpdate;

        public bool IsOn { get; private set; }

        public void SetPowered(bool state)
        {
            IsOn = state;
        }
        private void Awake()
        {
            if (!TryGetComponent(out InteractableStorage storage))
            {
                Destroy(this);
                return;
            }

            interactable = storage;
            _chargeRate = ChargeRateUnitsPerHour / 3600;

            Plugin.OnDragItemRequested += Plugin_OnDragItemRequested;
            Plugin.OnDropItemRequested += Plugin_OnDropItemRequested;
            Plugin.OnSwapItemRequested += Plugin_OnSwapItemRequested;
        }

        private void Plugin_OnSwapItemRequested(PlayerInventory inventory, ItemAsset item1, ItemAsset item2, ref bool shouldAllow)
        {
            if (!inventory.storage == (InteractableStorage)this.interactable)
                return;
        }

        private void Plugin_OnDropItemRequested(PlayerInventory inventory, ItemAsset asset, ref bool shouldAllow)
        {
            if (!inventory.storage == (InteractableStorage)this.interactable)
                return;

            if (asset.useableType != typeof(UseableVehicleBattery))
                shouldAllow = false;
        }

        private void Plugin_OnDragItemRequested(PlayerInventory inventory, ItemAsset asset, ref bool shouldAllow)
        {
            if (!inventory.storage == (InteractableStorage)this.interactable)
                return;

            if(asset.useableType != typeof(UseableVehicleBattery))
                shouldAllow = false;
        }

        private void OnDestroy()
        {
            Plugin.OnDragItemRequested -= Plugin_OnDragItemRequested;
            Plugin.OnDropItemRequested -= Plugin_OnDropItemRequested;
            Plugin.OnSwapItemRequested -= Plugin_OnSwapItemRequested;
        }
        private void Update()
        {
            if (!IsOn)
                return;

            if (timeSinceLastUpdate < 1)
            {
                timeSinceLastUpdate += Time.deltaTime;
                return;
            }

            _dueCharge += _chargeRate;
            if (_dueCharge < 1)
                return;

            var intcharge = (byte)Math.Round(_dueCharge);

            if(battery.item.quality + intcharge <= 100)
                battery.item.quality += intcharge;

        }
    }
}
