using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Linq;
using UnityEngine;
using Wired.Utilities;
using Wired.WiredInteractables;

namespace Wired.Wrappers
{
    public class ConsumerInteractable(Transform barricade)
    {
        private readonly InteractableSpot _spot = barricade.GetComponent<InteractableSpot>();
        private readonly InteractableOven _oven = barricade.GetComponent<InteractableOven>();
        private readonly InteractableOxygenator _oxygenator = barricade.GetComponent<InteractableOxygenator>();
        private readonly InteractableSafezone _safezone = barricade.GetComponent<InteractableSafezone>();
        private readonly InteractableCharge _charge = barricade.GetComponent<InteractableCharge>();
        private readonly IWiredInteractable _wiredInteractable = barricade.GetComponent<IWiredInteractable>();
        public bool IsPowered { get; private set; }
        public void SetPowered(bool powered)
        {
            if (_wiredInteractable != null)
            {
                _wiredInteractable.SetPowered(powered);
                IsPowered = powered;
                return;
            }
            if (_spot != null)
            {
                BarricadeManager.ServerSetSpotPowered(_spot, powered);
                IsPowered = powered;
            }
            if (_oven != null)
            {
                BarricadeManager.ServerSetOvenLit(_oven, powered);
                IsPowered = powered;
            }
            if (_oxygenator != null)
            {
                BarricadeManager.ServerSetOxygenatorPowered(_oxygenator, powered);
                IsPowered = powered;
            }
            if (_safezone != null)
            {
                BarricadeManager.ServerSetSafezonePowered(_safezone, powered);
                IsPowered = powered;
            }
            if (_charge != null && powered == true)
                _charge.Detonate(null);
        }

        
        public void Uninitialize()
        {
            _wiredInteractable?.Uninitialize();
        }
    }
}
