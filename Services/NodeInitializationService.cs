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
            AssetParser assetParser = new AssetParser(barricade.asset.getFilePath());
            if (assetParser.TryGetBool("Wired_Create_Node", out bool value))
            {
                if (!value) return;
            }

            if (_assets.WiredAssets.TryGetValue(barricade.asset.GUID, out IWiredAsset wiredAsset))
            {
                InitFromAsset(barricade, wiredAsset);
                return;
            }

            InitFromWhatever(barricade);
        }

        private void InitFromAsset(BarricadeDrop barricade, IWiredAsset asset)
        {
            IElectricNode createdNode = null;
            switch (asset)
            {
                case SwitchAsset switchAsset:
                    var sw = barricade.model.gameObject.AddComponent<GateNode>();
                    sw.Asset = switchAsset;
                    sw.SetPowered(false);
                    sw.SwitchableByPlayer = true;
                    createdNode = sw;
                    break;

                case PlayerDetectorAsset detectorAsset:
                    createdNode = InitializePlayerDetector(barricade, detectorAsset);
                    break;

                case KeypadAsset keypadAsset:
                    createdNode = InitializeKeypad(barricade, keypadAsset);
                    break;

                case RemoteReceiverAsset receiverAsset:
                    createdNode = InitializeRemoteReceiver(barricade, receiverAsset);
                    break;

                case RemoteTransmitterAsset transmitterAsset:
                    createdNode = InitializeRemoteTransmitter(barricade, transmitterAsset);
                    break;

                case TimerAsset timerAsset:
                    var timer = barricade.model.gameObject.AddComponent<TimerNode>();
                    timer.Asset = timerAsset;
                    timer.SetPowered(false);
                    timer.DelaySeconds = (ushort)Math.Round(timerAsset.DelaySeconds);
                    timer.StopIfRunning();
                    createdNode = timer;
                    break;

                case GeneratorAsset supplierAsset:
                    var sup = barricade.model.gameObject.AddComponent<SupplierNode>();
                    sup.Asset = supplierAsset;
                    sup.Supply = supplierAsset.Supply;
                    createdNode = sup;
                    break;

                case SolarPanelAsset solarPanelAsset:
                    var supplier = barricade.model.gameObject.AddComponent<SupplierNode>();
                    var solar = barricade.model.gameObject.AddComponent<SolarPanel>();
                    solar.Asset = solarPanelAsset;
                    supplier.Asset = solarPanelAsset;
                    if (solarPanelAsset.HasMovingPart)
                    {
                        Console.WriteLine($"Has moving part");
                        var movingPartGameObj = supplier.transform.Find("MovingPart");
                        if (movingPartGameObj == null)
                        {
                            WiredLogger.Error($"MovingPart transform of \"{barricade.asset.FriendlyName}\" is missing.");
                            return;
                        }

                        var bar = new Barricade(Assets.find(EAssetType.ITEM, solarPanelAsset.MovingPartId) as ItemBarricadeAsset);
                        if(bar == null)
                        {
                            WiredLogger.Error($"Couldn't find barricade asset for MovingPart of \"{barricade.asset.FriendlyName}\".");
                            return;
                        }

                        Transform movingPartTransform = BarricadeManager.dropNonPlantedBarricade(
                            bar,
                            movingPartGameObj.position,
                            barricade.model.rotation,
                            barricade.GetServersideData().owner,
                            barricade.GetServersideData().group
                        );
                        Console.WriteLine($"Moving part created at {movingPartTransform.position}, root position: {barricade.model.position}");

                        solar.MovingPart = movingPartTransform;
                        solar.PanelNormal = movingPartTransform.up;
                    }
                    else
                    {
                        Console.WriteLine($"No moving part");
                    }
                    createdNode = supplier;
                    break;

                case ConsumerAsset consumerAsset:
                    var cons = barricade.model.gameObject.AddComponent<ConsumerNode>();
                    cons.Asset = consumerAsset;
                    cons.SetPowered(false);
                    cons.Consumption = consumerAsset.Consumption;
                    createdNode = cons;
                    break;

                case NetworkDataDisplayAsset networkDataDisplayAsset:
                    var ndaa = barricade.model.gameObject.AddComponent<ConsumerNode>();
                    barricade.model.gameObject.AddComponent<NetworkDataDisplay>();
                    ndaa.Asset = networkDataDisplayAsset;
                    ndaa.SetPowered(false);
                    ndaa.Consumption = networkDataDisplayAsset.Consumption;
                    break;

                case BatteryChargerAsset batteryChargerAsset:
                    var bca = barricade.model.gameObject.AddComponent<ConsumerNode>();
                    barricade.model.gameObject.AddComponent<BatteryCharger>();
                    bca.Asset = batteryChargerAsset;
                    bca.SetPowered(false);
                    bca.Consumption = bca.Consumption;
                    break;
            }

            if (createdNode != null)
            {
                OnNodeCreated?.Invoke(barricade, createdNode);
            }
        }

        private GateNode InitializeKeypad(BarricadeDrop barricade, KeypadAsset keypadAsset)
        {
            var sw = barricade.model.gameObject.AddComponent<GateNode>();
            sw.SetPowered(false);
            sw.SwitchableByPlayer = false;

            var keypad = barricade.model.gameObject.AddComponent<Keypad>();
            keypad.StaysOnSeconds = keypadAsset.StaysOpenSeconds;
            return sw;
        }

        private GateNode InitializePlayerDetector(BarricadeDrop barricade, PlayerDetectorAsset asset)
        {
            var sw = barricade.model.gameObject.AddComponent<GateNode>();
            sw.SetPowered(false);
            sw.SwitchableByPlayer = false;

            var detectorObj = sw.transform.Find("Detector");
            if (detectorObj == null)
            {
                WiredLogger.Error($"Barricade \"{barricade.asset.FriendlyName}\" is a PlayerDetector but missing 'Detector' child. Destroying node.");
                Component.Destroy(sw);
                return null;
            }

            detectorObj.gameObject.SetActive(true);

            var detector = detectorObj.gameObject.AddComponent<PlayerDetector>();
            detector.Radius = asset.Radius;
            detector.Inverted = asset.Inverted;

            return sw;
        }

        private GateNode InitializeRemoteReceiver(BarricadeDrop barricade, RemoteReceiverAsset asset)
        {
            var sw = barricade.model.gameObject.AddComponent<GateNode>();
            sw.SetPowered(false);
            sw.SwitchableByPlayer = false;
            barricade.model.gameObject.AddComponent<RemoteReceiver>();
            return sw;
        }

        private ConsumerNode InitializeRemoteTransmitter(BarricadeDrop barricade, RemoteTransmitterAsset asset)
        {
            barricade.model.gameObject.AddComponent<RemoteTransmitter>().Range = asset.Range;
            var cons = barricade.model.gameObject.AddComponent<ConsumerNode>();
            cons.SetPowered(false);
            cons.Consumption = asset.Consumption;
            return cons;
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
                else
                    node.Consumption = 100f;

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
