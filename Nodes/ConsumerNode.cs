using SDG.Unturned;
using Steamworks;
using System.Collections;
using UnityEngine;

namespace Wired.Nodes
{
    public class ConsumerNode : Node
    {
        public uint Consumption { get; set; }
        private bool isPowered;

        private InteractableSpot _spot;
        private InteractableOven _oven;
        private InteractableOxygenator _oxygenator;
        private InteractableSafezone _safezone;
        private InteractableCharge _charge;
        private CoolConsumer _coolConsumer;

        protected override void Awake()
        {
            base.Awake();
            _spot = GetComponent<InteractableSpot>();
            _oven = GetComponent<InteractableOven>();
            _oxygenator = GetComponent<InteractableOxygenator>();
            _safezone = GetComponent<InteractableSafezone>();
            _charge = GetComponent<InteractableCharge>();
            _coolConsumer = GetComponent<CoolConsumer>();
        }
        public override void IncreaseVoltage(uint amount)
        {
            Voltage = amount;
            CheckPowerStatus();
        }

        public override void DecreaseVoltage(uint amount)
        {
            Voltage = (Voltage < amount) ? 0 : Voltage - amount;
            CheckPowerStatus();
        }

        private void CheckPowerStatus()
        {
            bool newPowered = Voltage >= Consumption;
            if (isPowered != newPowered)
            {
                isPowered = newPowered;
                SetPowered(isPowered);
            }
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
                        BarricadeManager.sendFuel(gen, 512);
                        BarricadeManager.ServerSetGeneratorPowered(gen.GetComponent<InteractableGenerator>(), true);
                    }
                }
            }
            if (_coolConsumer != null)
                _coolConsumer.SetActive(powered);
            if (_oven != null)
                BarricadeManager.ServerSetOvenLit(_oven, powered);
            if (_oxygenator != null)
                BarricadeManager.ServerSetOxygenatorPowered(_oxygenator, powered);
            if (_safezone != null)
                BarricadeManager.ServerSetSafezonePowered(_safezone, powered);
            if (_charge != null && powered)
                StartCoroutine(DelayedExplosion());
        }

        IEnumerator DelayedExplosion()
        {
            yield return new WaitUntil(() => Plugin.Instance.UpdateFinished);
            _charge.detonate((CSteamID)BarricadeManager.FindBarricadeByRootTransform(_charge.transform).GetServersideData().owner);
        }
    }
}