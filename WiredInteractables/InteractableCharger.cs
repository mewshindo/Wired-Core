using SDG.Unturned;
using System;
using UnityEngine;

namespace Wired.WiredInteractables
{
    public class InteractableCharger : MonoBehaviour, WiredInteractable
    {
        public Interactable interactable { get; private set; }
        private ItemJar battery;

        public bool isOn { get; private set; }

        public void Toggle(bool state)
        {
            isOn = state;
        }
        private void Awake()
        {
            if (!TryGetComponent(out InteractableStorage storage))
            {
                Destroy(this);
            }

            interactable = storage;
            Plugin.OnDragItemRequested += Plugin_OnDragItemRequested;
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

        }
        private void Update()
        {

        }
        private void UpdateStorage(ItemJar jar)
        {
            foreach(ItemJar item in ((InteractableStorage)interactable).items.items)
            {
                if(item.item.GetAsset().useableType == typeof(UseableVehicleBattery))
                {

                }
            }
        }
    }
}
