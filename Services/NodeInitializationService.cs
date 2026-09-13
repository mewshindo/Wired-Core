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

            BarricadeFinder bf = new();
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
            AssetParser assetParser = new(barricade.asset.getFilePath());
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
                    {
                        var sw = barricade.model.gameObject.AddComponent<GateNode>();
                        sw.barricade = barricade;
                        sw.Asset = switchAsset;
                        sw.SetPowered(false);
                        sw.SwitchableByPlayer = true;
                        createdNode = sw;
                        break;
                    }
                case ButtonAsset buttonAsset:
                    {
                        var ba = barricade.model.gameObject.AddComponent<GateNode>();
                        ba.barricade = barricade;
                        ba.Asset = buttonAsset;
                        ba.SetPowered(false);
                        ba.Switch(false);
                        ba.SwitchableByPlayer = true;
                        var button = barricade.model.gameObject.AddComponent<Button>();
                        button.StaysPressedSeconds = buttonAsset.StaysPressedSecons;
                        createdNode = ba;
                        break;
                    }
                case LogicGateAsset logicGate:
                    {
                        var gn = barricade.model.gameObject.AddComponent<GateNode>();
                        gn.barricade = barricade;
                        gn.Asset = logicGate;
                        gn.Switch(false);
                        gn.SwitchableByPlayer = false;
                        var lg = gn.gameObject.AddComponent<LogicGate>();
                        lg.Type = logicGate.Type;
                        createdNode = gn;
                        break;
                    }
                case ConnectorAsset connectorAsset:
                    {
                        var cn = barricade.model.gameObject.AddComponent<GateNode>();
                        cn.barricade = barricade;
                        cn.Asset = connectorAsset;
                        cn.Switch(true);
                        createdNode = cn;
                        break;
                    }
                case PlayerDetectorAsset detectorAsset:
                    {
                        createdNode = InitializePlayerDetector(barricade, detectorAsset);
                        break;
                    }
                case KeypadAsset keypadAsset:
                    {
                        createdNode = InitializeKeypad(barricade, keypadAsset);
                        break;
                    }
                case RemoteReceiverAsset receiverAsset:
                    {
                        createdNode = InitializeRemoteReceiver(barricade, receiverAsset);
                        break;
                    }
                case RemoteTransmitterAsset transmitterAsset:
                    {
                        createdNode = InitializeRemoteTransmitter(barricade, transmitterAsset);
                        break;
                    }
                case TimerAsset timerAsset:
                    {
                        var timer = barricade.model.gameObject.AddComponent<TimerNode>();
                        timer.barricade = barricade;
                        timer.Asset = timerAsset;
                        timer.SetPowered(false);
                        timer.DelaySeconds = (ushort)Math.Round(timerAsset.DelaySeconds);
                        timer.StopIfRunning();
                        createdNode = timer;
                        break;
                    }
                case GeneratorAsset supplierAsset:
                    {
                        var sup = barricade.model.gameObject.AddComponent<SupplierNode>();
                        sup.barricade = barricade;
                        sup.Asset = supplierAsset;
                        sup.Supply = supplierAsset.Supply;
                        createdNode = sup;
                        break;
                    }
                case SolarPanelAsset solarPanelAsset:
                    {
                        var supplier = barricade.model.gameObject.AddComponent<SupplierNode>();
                        supplier.barricade = barricade;
                        var solar = barricade.model.gameObject.AddComponent<SolarPanel>();
                        supplier.Asset = solarPanelAsset;
                        createdNode = supplier;
                        break;
                    }
                case WindTurbineAsset windTurbineAsset:
                    {
                        var supplier = barricade.model.gameObject.AddComponent<SupplierNode>();
                        supplier.barricade = barricade;
                        var windturbine = barricade.model.gameObject.AddComponent<WindTurbine>();
                        supplier.Asset = windTurbineAsset;
                        createdNode = supplier;
                        break;
                    }
                case BatteryAsset batteryAsset:
                    {
                        var supp = barricade.model.gameObject.AddComponent<SupplierNode>();
                        supp.barricade = barricade;
                        supp.Asset = batteryAsset;
                        var battery = barricade.model.gameObject.AddComponent<Battery>();
                        break;
                    }
                case ConsumerAsset consumerAsset:
                    {
                        var cons = barricade.model.gameObject.AddComponent<ConsumerNode>();
                        cons.barricade = barricade;
                        cons.Asset = consumerAsset;
                        cons.Consumption = consumerAsset.Consumption;
                        cons.SetPowered(false);
                        createdNode = cons;
                        break;
                    }
                case SprinklerAsset sprinklerAsset:
                    {
                        var spr = barricade.model.gameObject.AddComponent<ConsumerNode>();
                        spr.barricade = barricade;
                        spr.Asset = sprinklerAsset;
                        spr.Consumption = sprinklerAsset.Consumption;
                        barricade.model.gameObject.AddComponent<Sprinkler>();
                        spr.SetPowered(false);
                        createdNode = spr;
                        break;
                    }
                case NetworkAnalyzerAsset networkDataDisplayAsset:
                    {
                        var ndaa = barricade.model.gameObject.AddComponent<ConsumerNode>();
                        ndaa.barricade = barricade;
                        ndaa.Asset = networkDataDisplayAsset;
                        ndaa.Consumption = networkDataDisplayAsset.Consumption;
                        barricade.model.gameObject.AddComponent<NetworkAnalyzer>();
                        ndaa.SetPowered(false);
                        createdNode = ndaa;
                        break;
                    }
                case DaylightSensorAsset dayNightSensorAsset:
                    {
                        var gate = barricade.model.gameObject.AddComponent<GateNode>();
                        gate.Asset = dayNightSensorAsset;
                        gate.SetPowered(false);
                        var dnsa = barricade.model.gameObject.AddComponent<DaylightSensor>();
                        break;
                    }
                case BatteryChargerAsset batteryChargerAsset:
                    {
                        var bca = barricade.model.gameObject.AddComponent<ConsumerNode>();
                        bca.barricade = barricade;
                        barricade.model.gameObject.AddComponent<BatteryCharger>();
                        bca.Asset = batteryChargerAsset;
                        bca.SetPowered(false);
                        bca.Consumption = bca.Consumption;
                        createdNode = bca;
                        break;
                    }
            }
            WiredLogger.Info($"Initialized WiredAsset {barricade.asset.FriendlyName} as {asset}");
            BarricadeFinder finder = new(position: barricade.model.position);
            if (!finder.GetBarricadesInRadius(radius: 128).Any(b => b.asset != null && b.asset.GUID == Plugin.Instance.Resources.generator_technical.GUID))
            {
                Barricade bar = new(Plugin.Instance.Resources.generator_technical);
                Transform gen = BarricadeManager.dropNonPlantedBarricade(bar, barricade.model.transform.position, barricade.model.transform.rotation, 0, 0);
                var gen2 = gen.GetComponent<InteractableGenerator>();
                gen2.askFill(1024);
                BarricadeManager.sendFuel(gen, gen2.fuel);
                BarricadeManager.ServerSetGeneratorPowered(gen.GetComponent<InteractableGenerator>(), true);
            }

            if (createdNode != null)
            {
                OnNodeCreated?.Invoke(barricade, createdNode);
            }
        }

        private GateNode InitializeKeypad(BarricadeDrop barricade, KeypadAsset keypadAsset)
        {
            var sw = barricade.model.gameObject.AddComponent<GateNode>();
            sw.barricade = barricade;
            sw.SetPowered(false);
            sw.SwitchableByPlayer = false;

            var keypad = barricade.model.gameObject.AddComponent<Keypad>();
            keypad.StaysOnSeconds = keypadAsset.StaysOpenSeconds;
            return sw;
        }

        private GateNode InitializePlayerDetector(BarricadeDrop barricade, PlayerDetectorAsset asset)
        {
            var sw = barricade.model.gameObject.AddComponent<GateNode>();
            sw.barricade = barricade;

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
            sw.barricade = barricade;

            sw.SetPowered(false);
            sw.SwitchableByPlayer = false;
            barricade.model.gameObject.AddComponent<RemoteReceiver>();
            return sw;
        }

        private ConsumerNode InitializeRemoteTransmitter(BarricadeDrop barricade, RemoteTransmitterAsset asset)
        {
            barricade.model.gameObject.AddComponent<RemoteTransmitter>().Range = asset.Range;
            var cons = barricade.model.gameObject.AddComponent<ConsumerNode>();
            cons.barricade = barricade;

            cons.SetPowered(false);
            cons.Consumption = asset.Consumption;
            return cons;
        }

        private void InitFromWhatever(BarricadeDrop barricade)
        {
            if (barricade.model.TryGetComponent(out InteractableGenerator _))
            {
                AssetParser parser = new(barricade.asset.getFilePath());
                var node = barricade.model.gameObject.AddComponent<SupplierNode>();
                node.barricade = barricade;
                node.SetPowered(false);
                if (parser.TryGetFloat("Power_Supply", out float supply))
                    node.Supply = supply;

                OnNodeCreated?.Invoke(barricade, node);
                return;
            }

            if (IsConsumer(barricade.model))
            {
                AssetParser parser = new(barricade.asset.getFilePath());
                var node = barricade.model.gameObject.AddComponent<ConsumerNode>();
                node.barricade = barricade;
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
                   model.GetComponent<InteractableCharge>() != null ||
                   model.GetComponent<InteractableSentry>() != null;
        }
    }
}
