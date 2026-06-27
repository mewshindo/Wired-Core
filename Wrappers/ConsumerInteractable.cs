using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Wired.Utilities;
using Wired.WiredInteractables;

namespace Wired.Wrappers
{
    public class ConsumerInteractable
    {
        private InteractableSpot _spot;
        private InteractableOven _oven;
        private InteractableOxygenator _oxygenator;
        private InteractableSafezone _safezone;
        private InteractableCharge _charge;
        private IWiredInteractable _wiredInteractable;
        public ConsumerInteractable(Transform barricade)
        {
            _wiredInteractable = barricade.GetComponent<IWiredInteractable>();
            _spot = barricade.GetComponent<InteractableSpot>();
            _oven = barricade.GetComponent<InteractableOven>();
            _oxygenator = barricade.GetComponent<InteractableOxygenator>();
            _safezone = barricade.GetComponent<InteractableSafezone>();
            _charge = barricade.GetComponent<InteractableCharge>();
        }

        public void SetPowered(bool powered)
        {
            if (_wiredInteractable != null)
            {
                _wiredInteractable.SetPowered(powered);
                WiredLogger.Info($"Set wired interactable {(_wiredInteractable.interactable != null ? _wiredInteractable.interactable.name : "null")} to {(powered ? "ON" : "OFF")}");
            }
            if (_spot != null)
            {
                BarricadeManager.ServerSetSpotPowered(_spot, powered);

                if (!_spot.isWired)
                {
                    Barricade bar = new Barricade(Plugin.Instance.Resources.generator_technical);
                    Transform gen = BarricadeManager.dropNonPlantedBarricade(bar, _spot.transform.position, _spot.transform.rotation, 0, 0);
                    if (gen != null)
                    {
                        BarricadeManager.sendFuel(gen, 512);
                        BarricadeManager.ServerSetGeneratorPowered(gen.GetComponent<InteractableGenerator>(), true);
                    }
                }
            }
            if (_oven != null)
                BarricadeManager.ServerSetOvenLit(_oven, powered);
            if (_oxygenator != null)
                BarricadeManager.ServerSetOxygenatorPowered(_oxygenator, powered);
            if (_safezone != null)
                BarricadeManager.ServerSetSafezonePowered(_safezone, powered);
            if (_charge != null)
                _charge.Detonate(UnturnedPlayer.FromCSteamID(new CSteamID(_charge.owner)).Player);
        }
    }
}
