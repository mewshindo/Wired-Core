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
using Wired.WiredInteractables;

namespace Wired.Services
{
    public class NodeInitializationService
    {
        private Resources _resources;

        public delegate void NodeCreated(BarricadeDrop drop, IElectricNode node);
        public static event NodeCreated OnNodeCreated;
        public NodeInitializationService(Resources resources)
        {
            _resources = resources;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;

            BarricadeFinder bf = new BarricadeFinder();
            foreach (BarricadeRegion reg in BarricadeManager.regions)
            {
                for (int i = 0; i < reg.drops.Count; i++)
                {
                    var drop = reg.drops[i];
                    if (drop == null)
                        continue;
                    OnBarricadeSpawned(reg, drop);
                }
            }
        }
        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if(drop.IsChildOfVehicle)
                return;

            TryInializeBarricade(drop);
        }


        private void TryInializeBarricade(BarricadeDrop barricade)
        {
            AssetParser parser = new AssetParser(barricade.asset.getFilePath());

            if (_resources.WiredAssets.ContainsKey(barricade.asset.GUID))
            {
                switch (_resources.WiredAssets[barricade.asset.GUID].Type)
                {
                    case WiredAssetType.Switch:
                        var sw = barricade.model.gameObject.AddComponent<SwitchNode>();
                        sw.SetPowered(false);
                        if(parser.HasEntry("WiredType PlayerDetector"))
                        {
                            sw.SwitchableByPlayer = false;
                            var obj = sw.transform.Find("Detector");
                            if(obj != null)
                            {
                                var detector = obj.gameObject.AddComponent<PlayerDetector>();
                                if (parser.HasEntry("DetectorConfig"))
                                {
                                    if(parser.TryGetString("DetectorType", out string type))
                                    {
                                        detector.DetectorType = type == "Line" ? PlayerDetectorType.Line : PlayerDetectorType.Sphere;
                                    }
                                    if(parser.TryGetFloat("Radius", out float radius))
                                        detector.Radius = radius;
                                    if(parser.HasEntry("Inverted"))
                                        detector.Inverted = true;
                                }
                                else
                                {
                                    detector.DetectorType = PlayerDetectorType.Sphere;
                                    detector.Radius = 5;
                                    WiredLogger.Warn($"Player detector {barricade.asset.FriendlyNameWithFriendlyType} is not configured, assigning default config: type sphere, radius 5m.");
                                }
                            }
                            else
                            {
                                WiredLogger.Error($"Barricade \"{barricade.asset.FriendlyNameWithFriendlyType}\" is configured like a player detector but doesn't have \"Detector\" gameobject, destroying node.");
                                Component.Destroy(sw);
                                return;
                            }
                        }
                        OnNodeCreated?.Invoke(barricade, sw);
                        return;

                    case WiredAssetType.Timer:
                        var timer = barricade.model.gameObject.AddComponent<TimerNode>();
                        timer.SetPowered(false);
                        if (parser.TryGetFloat("Timer_Delay_Seconds", out float delay))
                        {
                            timer.DelaySeconds = (ushort)Math.Round(delay);
                        }
                        else
                        {
                            timer.DelaySeconds = 5;
                        }
                        timer.StopIfRunning();
                        OnNodeCreated?.Invoke(barricade, timer);
                        return;

                    case WiredAssetType.RemoteTransmitter:
                        return;

                    case WiredAssetType.RemoteReceiver:
                        return;


                    default:
                        break;
                }
            }

            if(barricade.model.TryGetComponent(out InteractableGenerator _))
            {
                var node = barricade.model.gameObject.AddComponent<SupplierNode>();
                node.SetPowered(false);
                if (parser.TryGetFloat("Power_Supply", out float supply))
                    node.Supply = supply;
                OnNodeCreated?.Invoke(barricade, node);
                return;
            }

            if (IsConsumer(barricade.model))
            {
                var node = barricade.model.gameObject.AddComponent<ConsumerNode>();
                node.SetPowered(false);

                if (parser.TryGetFloat("Power_Consumption", out float consumption))
                    node.Consumption = consumption;

                if (barricade.asset.id == 459) // Spotlight
                    node.Consumption = 250;
                if (barricade.asset.id == 1222) // Cagelight
                    node.Consumption = 25;
                if (barricade.asset.id == 1241) // Charge
                    node.Consumption = 5;

                if(parser.HasEntry("WiredType BatteryCharger"))
                {
                    var charger = barricade.model.gameObject.AddComponent<BatteryCharger>();
                    if(parser.TryGetFloat("ChargePerHour", out float chargerate))
                        charger.ChargeRateUnitsPerHour = chargerate;
                }

                OnNodeCreated?.Invoke(barricade, node);
            }
        }

        private bool IsConsumer(Transform barricade)
        {
            if (barricade == null) return false;

            if (barricade.GetComponent<InteractableSpot>() != null)
                return true;
            if (barricade.GetComponent<InteractableOven>() != null)
                return true;
            if (barricade.GetComponent<InteractableOxygenator>() != null)
                return true;
            if (barricade.GetComponent<InteractableSafezone>() != null)
                return true;
            if (barricade.GetComponent<InteractableCharge>() != null)
                return true;

            return false;
        }
    }
}
