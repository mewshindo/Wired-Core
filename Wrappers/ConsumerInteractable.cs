using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Wired.Wrappers
{
    public class ConsumerInteractable
    {
        private InteractableSpot _spot;
        private InteractableOven _oven;
        private InteractableOxygenator _oxygenator;
        private InteractableSafezone _safezone;
        private InteractableCharge _charge;
        public ConsumerInteractable(Transform barricade)
        {
            _spot = barricade.GetComponent<InteractableSpot>();
            _oven = barricade.GetComponent<InteractableOven>();
            _oxygenator = barricade.GetComponent<InteractableOxygenator>();
            _safezone = barricade.GetComponent<InteractableSafezone>();
            _charge = barricade.GetComponent<InteractableCharge>();
        }

        public void SetPowered(bool powered)
        {
            if (_spot != null)
            {
                BarricadeManager.ServerSetSpotPowered(_spot, powered);

                if (!_spot.isWired)
                {
                    Barricade bar = new Barricade(Plugin.Instance.Resources.generator_technical);
                    Transform gen = BarricadeManager.dropNonPlantedBarricade(bar, _spot.transform.position, _spot.transform.rotation, 0, 0);
                    if (gen != null)
                    {
                        BarricadeManager.sendFuel(gen, 2048);
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
