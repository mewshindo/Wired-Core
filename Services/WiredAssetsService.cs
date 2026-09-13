using Rocket.Core.Assets;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Utilities;
using Wired.WiredAssets;
using Wired.WiredInteractables;

namespace Wired.Services
{
    public class WiredAssetsService
    {

        public Dictionary<Guid, IWiredAsset> WiredAssets = [];

        private readonly List<IWiredAsset> _defaultAssets = // Vanilla assets, probly gotta put this in a config
        [
            new GeneratorAsset(new Guid("dc56734a150849e785975751364d41de"), 2800), // Industrial Generator
            new GeneratorAsset(new Guid("72fae83175f34bde94bd52d40c7a9ebc"), 400), // Portable Generator

            new ConsumerAsset(new Guid("3407a91dde0c4454b91d5af072f11a4c"), 100), // Spotlight
            new ConsumerAsset(new Guid("9908a43237364f22a62242cd1fb14fc9"), 100), // Cagelight
            new ConsumerAsset(new Guid("eeffac0063804866b38c8eb6436ace10"), 1000), // Oil Pump
            new ConsumerAsset(new Guid("d673b381629a45d9b0c5889f505374be"), 200), // Electric Stove
            new ConsumerAsset(new Guid("d3c40816534e48c3af0e26fb4d5f1b1a"), 25), // Clock
            new ConsumerAsset(new Guid("1f8856edf5964774aa2457b37e45603b"), 400), // Safezone Radiator
            new ConsumerAsset(new Guid("ea56c734f3614983a5381bbce91ba79a"), 400), // Oxygenator

            new ConsumerAsset(new Guid("5a7281e9af4c435b8cb7f9f5acf05aa9"), 400), // Friendly Sentry
            new ConsumerAsset(new Guid("0c005ae5e4c74575af53ffe2144fad0b"), 400), // Neutral Sentry
            new ConsumerAsset(new Guid("8f7670d2dabc449d8f38c8cb656c6acf"), 400), // Hostile Sentry
        ];
        public WiredAssetsService()
        {
            WiredAssets.Concat(_defaultAssets.ToDictionary(a => a.GUID, a => a));
            PopulateAssets();

            foreach (var asset in _defaultAssets)
            {
                if (!WiredAssets.ContainsKey(asset.GUID))
                {
                    WiredAssets.Add(asset.GUID, asset);
                    WiredLogger.Info($"Added default wired asset: ({asset.GUID})");
                }
            }
        }
        private void PopulateAssets()
        {
            List<ItemAsset> items = [];
            Assets.find(items);

            foreach (ItemAsset asset in items)
            {
                AssetParser parser = new(asset.getFilePath());
                string[] stringstoparse = 
                [
                    "WiredType WiringTool",
                    "WiredType RemoteTool",
                    "WiredType ManualTablet",
                    "WiredType EngineeringGoggles",
                    "WiredType Consumer",
                    "WiredType Supplier",
                    "WiredType Gate",
                ];
                if (parser.HasAnyEntry(stringstoparse, out var foundentry))
                {
                    switch (foundentry.Split(' ')[1])
                    {
                        case "WiringTool":
                            WiredAssets.Add(asset.GUID, new WiringToolAsset(asset.GUID));
                            break;
                        case "RemoteTool":
                            WiredAssets.Add(asset.GUID, new RemoteToolAsset(asset.GUID));
                            break;
                        case "ManualTablet":
                            WiredAssets.Add(asset.GUID, new ManualTabletAsset(asset.GUID));
                            break;
                        case "EngineeringGoggles":
                            WiredAssets.Add(asset.GUID, new EngineerGogglesAsset(asset.GUID));
                            break;
                        case "Gate":
                            PopulateGate(parser, asset);
                            break;
                        case "Consumer":
                            PopulateConsumer(parser, asset);
                            break;
                        case "Supplier":
                            PopulateSupplier(parser, asset);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void PopulateGate(AssetParser parser, ItemAsset asset)
        {
            if (parser.HasEntry("WiredBuild Switch"))
            {
                WiredAssets.Add(asset.GUID, new SwitchAsset(asset.GUID, true));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build Switch");
            }
            else if(parser.HasEntry("WiredBuild Button"))
            {
                WiredAssets.Add(asset.GUID,
                    new ButtonAsset(
                    guid: asset.GUID,
                    staysPressedSecons: parser.TryGetFloat("StaysPressed_Seconds", out float sps) ? sps : 2f));

                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build Button");
            }
            else if (parser.HasEntry("WiredBuild PlayerDetector"))
            {
                WiredAssets.Add(asset.GUID,
                new PlayerDetectorAsset(
                guid: asset.GUID,
                radius: parser.TryGetFloat("Radius", out float radius) ? radius : 3f));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build PlayerDetector");
            }
            else if (parser.HasEntry("WiredBuild Timer"))
            {
                WiredAssets.Add(asset.GUID,
                new TimerAsset(
                guid: asset.GUID,
                delayseconds: parser.TryGetFloat("Timer_Delay_Seconds", out float delay) ? delay : 5));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build Timer");
            }
            else if (parser.HasEntry("WiredBuild RemoteReceiver"))
            {
                WiredAssets.Add(asset.GUID, new RemoteReceiverAsset(asset.GUID));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build RemoteReceiver");
            }
            else if (parser.HasEntry("WiredBuild Keypad"))
            {
                WiredAssets.Add(asset.GUID, 
                new KeypadAsset(
                guid: asset.GUID,
                staysOpenSeconds: parser.TryGetFloat("StaysOpenSeconds", out float seconds) ? seconds : 3));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build Keypad");
            }
            else if (parser.HasEntry("WiredBuild Connector"))
            {
                WiredAssets.Add(asset.GUID,
                    new ConnectorAsset(
                    guid: asset.GUID));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build Connector");
            }
            else if (parser.HasEntry("WiredBuild LogicGate"))
            {
                if(parser.TryGetString("LogicGateType", out string lgt))
                {
                    if(Enum.TryParse(lgt, out LogicGateType type))
                    {
                        WiredAssets.Add(asset.GUID,
                            new LogicGateAsset(guid: asset.GUID, type));
                    }
                    else
                    {
                        WiredLogger.Error($"Logic Gate asset {asset.FriendlyName} has invalid Wired configuration.");
                    }
                }
                else
                {
                    WiredLogger.Error($"Logic Gate asset {asset.FriendlyName} has invalid Wired configuration.");
                }
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build LogicGate");
            }
            else if (parser.HasEntry("DaylightSensor"))
            {
                WiredAssets.Add(asset.GUID,
                new DaylightSensorAsset(
                guid: asset.GUID,
                mode: parser.TryGetString("Mode", out string mode) ? (mode == "Night" ? DaylightSensorMode.Night : DaylightSensorMode.Day) : DaylightSensorMode.Day));
                WiredLogger.Info($"Found wired asset: {asset.name} ({asset.GUID}) as Type Gate; Build DaylightSensor");
            }
        }
        private void PopulateConsumer(AssetParser parser, ItemAsset asset)
        {
            var consumption = parser.TryGetFloat("Power_Consumption", out float cons) ? cons : 0f;
            if(parser.HasEntry("WiredBuild RemoteTransmitter"))
            {
                var range = parser.TryGetFloat("Transmitter_Range_Meters", out float ran) ? ran : 50f;
                WiredAssets.Add(asset.GUID, new RemoteTransmitterAsset(asset.GUID, cons, range));
                return;
            }
            if(parser.HasEntry("WiredBuild NetworkAnalyzer"))
            {
                WiredAssets.Add(asset.GUID, new 
                    NetworkAnalyzerAsset(
                    asset.GUID, 
                    consumption, 
                    parser.TryGetFloat("DisplayBarricadeID", out float did) ? (ushort)did : (ushort)0));
                return;
            }
            if(parser.HasEntry("WiredBuild Sprinkler"))
            {
                WiredAssets.Add(asset.GUID, new
                    SprinklerAsset(
                    asset.GUID,
                    consumption,
                    parser.TryGetFloat("Effective_Radius_Meters", out float radius) ? radius : 4f));
                return;
            }
            if(parser.HasEntry("WiredBuild BatteryCharger"))
            {
                var chargerate = parser.TryGetFloat("BatteryCharger_ChargePerHour", out float cpr) ? cpr : 100f;
                WiredAssets.Add(asset.GUID, new BatteryChargerAsset(asset.GUID, chargerate));
                return;
            }
            WiredAssets.Add(asset.GUID, new ConsumerAsset(asset.GUID, consumption));
        }
        private void PopulateSupplier(AssetParser parser, ItemAsset asset)
        {
            var supply = parser.TryGetFloat("Power_Supply", out float supp) ? supp : 100f;

            if(parser.HasEntry("WiredBuild SolarPanel"))
            {
                var nightmodifier = parser.TryGetFloat("NightSupplyModifier", out float nmod) ? nmod : 0f;
                var movingpartid = parser.TryGetFloat("MovingPart_ID", out float mpid) ? mpid : (ushort)0;
                var movingpartminangle = parser.TryGetFloat("MovingPart_MaxAngle", out float mpmina) ? mpmina : 0f;
                WiredAssets.Add(asset.GUID, new SolarPanelAsset(asset.GUID, supply, nightmodifier, movingpartminangle, (ushort)movingpartid));
                return;
            }
            if(parser.HasEntry("WiredBuild WindTurbine"))
            {
                WiredAssets.Add(asset.GUID, new WindTurbineAsset(asset.GUID, supply));
                return;
            }
            if(parser.HasEntry("WiredBuild Battery"))
            {
                var capacity = parser.TryGetFloat($"Capacity", out float capa) ? capa : 100f;
                var maxburn = parser.TryGetFloat($"MaxBurnPerSecond", out float mbps) ? mbps : 1f;
                WiredAssets.Add(asset.GUID, new BatteryAsset(asset.GUID, supply, capacity, maxburn));
                return;
            }

            WiredAssets.Add(asset.GUID, new GeneratorAsset(asset.GUID, supply));
        }
    }
}