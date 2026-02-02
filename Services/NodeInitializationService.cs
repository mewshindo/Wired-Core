using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Models;
using Wired.Utilities;
using Wired.WiredAssets;
using Wired.WiredInteractables;

namespace Wired.Services
{
    public class NodeInitializationService
    {
        private readonly WiredAssetsService _assets;

        public delegate void NodeCreated(BarricadeDrop drop, IElectricNode node);
        public static event NodeCreated OnNodeCreated;

        public NodeInitializationService(WiredAssetsService resources)
        {
            _assets = resources;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;

            // Initialize existing barricades
            BarricadeFinder bf = new BarricadeFinder();
            foreach (BarricadeRegion reg in BarricadeManager.regions)
            {
                for (int i = 0; i < reg.drops.Count; i++)
                {
                    var drop = reg.drops[i];
                    if (drop != null)
                    {
                        OnBarricadeSpawned(reg, drop);
                    }
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.IsChildOfVehicle) return;
            TryInitializeBarricade(drop);
        }

        private void TryInitializeBarricade(BarricadeDrop barricade)
        {
            // 1. Check if we have a defined Cached Asset for this GUID
            if (_assets.WiredAssets.TryGetValue(barricade.asset.GUID, out IWiredAsset wiredAsset))
            {
                InitFromAsset(barricade, wiredAsset);
                return;
            }

            //InitFromWhatever(barricade);
        }

        private void InitFromAsset(BarricadeDrop barricade, IWiredAsset asset)
        {
            IElectricNode createdNode = null;

            switch (asset)
            {
                case SwitchAsset switchAsset:
                    var sw = barricade.model.gameObject.AddComponent<SwitchNode>();
                    sw.Asset = switchAsset;
                    sw.SetPowered(false);
                    sw.SwitchableByPlayer = true;
                    createdNode = sw;
                    break;

                case PlayerDetectorAsset detectorAsset:
                    createdNode = InitializePlayerDetector(barricade, detectorAsset);
                    break;

                case TimerAsset timerAsset:
                    var timer = barricade.model.gameObject.AddComponent<TimerNode>();
                    timer.Asset = timerAsset;
                    timer.SetPowered(false);
                    timer.DelaySeconds = (ushort)Math.Round(timerAsset.DelaySeconds);
                    timer.StopIfRunning();
                    createdNode = timer;
                    break;

                case SupplierAsset supplierAsset:
                    var sup = barricade.model.gameObject.AddComponent<SupplierNode>();
                    sup.Asset = supplierAsset;
                    sup.SetPowered(false);
                    sup.Supply = supplierAsset.Supply;
                    createdNode = sup;
                    break;

                case ConsumerAsset consumerAsset:
                    var cons = barricade.model.gameObject.AddComponent<ConsumerNode>();
                    cons.Asset = consumerAsset;
                    cons.SetPowered(false);
                    cons.Consumption = consumerAsset.Consumption;
                    createdNode = cons;
                    break;
            }

            if (createdNode != null)
            {
                OnNodeCreated?.Invoke(barricade, createdNode);
            }
        }

        private SwitchNode InitializePlayerDetector(BarricadeDrop barricade, PlayerDetectorAsset asset)
        {
            var sw = barricade.model.gameObject.AddComponent<SwitchNode>();
            sw.SetPowered(false);
            sw.SwitchableByPlayer = false;

            var detectorObj = sw.transform.Find("Detector");
            if (detectorObj == null)
            {
                WiredLogger.Error($"Barricade \"{barricade.asset.FriendlyName}\" is a PlayerDetector but missing 'Detector' child. Destroying node.");
                Component.Destroy(sw);
                return null;
            }

            var detector = detectorObj.gameObject.AddComponent<PlayerDetector>();
            detector.Radius = asset.Radius;
            detector.Inverted = asset.Inverted;

            return sw;
        }

        private void InitFromWhatever(BarricadeDrop barricade)
        {
            if (barricade.model.TryGetComponent(out InteractableGenerator _))
            {
                AssetParser parser = new AssetParser(barricade.asset.getFilePath());
                var node = barricade.model.gameObject.AddComponent<SupplierNode>();
                node.SetPowered(false);
                if (parser.TryGetFloat("Power_Supply", out float supply))
                    node.Supply = supply;

                OnNodeCreated?.Invoke(barricade, node);
                return;
            }

            if (IsConsumer(barricade.model))
            {
                AssetParser parser = new AssetParser(barricade.asset.getFilePath());
                var node = barricade.model.gameObject.AddComponent<ConsumerNode>();
                node.SetPowered(false);

                if (parser.TryGetFloat("Power_Consumption", out float consumption))
                    node.Consumption = consumption;

                if (parser.HasEntry("WiredType BatteryCharger"))
                {
                    var charger = barricade.model.gameObject.AddComponent<BatteryCharger>();
                    if (parser.TryGetFloat("ChargePerHour", out float chargerate))
                        charger.ChargeRateUnitsPerHour = chargerate;
                }

                OnNodeCreated?.Invoke(barricade, node);
            }
        }

        private bool IsConsumer(Transform model)
        {
            if (model == null) return false;
            return model.GetComponent<InteractableSpot>() != null ||
                   model.GetComponent<InteractableOven>() != null ||
                   model.GetComponent<InteractableOxygenator>() != null ||
                   model.GetComponent<InteractableSafezone>() != null ||
                   model.GetComponent<InteractableCharge>() != null;
        }
    }
}
