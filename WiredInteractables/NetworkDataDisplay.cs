using SDG.Unturned;
using UnityEngine;
using Wired.Models;
using Wired.Utilities;

namespace Wired.WiredInteractables
{
    internal class NetworkDataDisplay : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }

        public bool IsOn { get; }

        public void SetPowered(bool state)
        {

        }

        private void Awake()
        {
            if (!TryGetComponent(out InteractableSign sign))
            {
                Destroy(this);
                return;
            }
            interactable = sign;

            ElectricNetwork.PowerUpdated += PowerUpdated;
        }
        private void OnDestroy()
        {
            ElectricNetwork.PowerUpdated -= PowerUpdated;
        }

        private void PowerUpdated(ElectricNetwork obj)
        {
            if (!IsOn)
                return;
            if (!obj.Nodes.Contains(this.GetComponent<ConsumerNode>())) // if the network updated is the one this thing is in
                return;

            BarricadeManager.ServerSetSignText((InteractableSign)interactable, 
                $"Supply {obj.TotalSupply}pu<br>" +
                $"Demand {obj.TotalConsumption}pu<br>" +
                $"Nodes count: {obj.Nodes.Count}<br>" +
                $"");
        }
    }
}
