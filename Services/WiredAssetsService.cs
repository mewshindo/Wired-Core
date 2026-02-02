using Rocket.Core.Assets;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wired.Utilities;
using Wired.WiredAssets;

namespace Wired.Services
{
    public class WiredAssetsService
    {

        public Dictionary<Guid, IWiredAsset> WiredAssets = new Dictionary<Guid, IWiredAsset>();

        private List<IWiredAsset> _defaultAssets = new List<IWiredAsset>() // Vanilla assets, probly gotta put this in a config
        {
            new SupplierAsset(new Guid("dc56734a150849e785975751364d41de"), 2800), // Industrial Generator
            new SupplierAsset(new Guid("72fae83175f34bde94bd52d40c7a9ebc"), 400), // Portable Generator

            new ConsumerAsset(new Guid("3407a91dde0c4454b91d5af072f11a4c"), 100), // Spotlight
            new ConsumerAsset(new Guid("9908a43237364f22a62242cd1fb14fc9"), 100), // Cagelight
            new ConsumerAsset(new Guid("eeffac0063804866b38c8eb6436ace10"), 1000), // Oil Pump
            new ConsumerAsset(new Guid("d673b381629a45d9b0c5889f505374be"), 200), // Electric Stove
            new ConsumerAsset(new Guid("d3c40816534e48c3af0e26fb4d5f1b1a"), 25), // Clock
            new ConsumerAsset(new Guid("1f8856edf5964774aa2457b37e45603b"), 400), // Safezone Radiator
            new ConsumerAsset(new Guid("ea56c734f3614983a5381bbce91ba79a"), 400), // Oxygenator

            new EngineerGogglesAsset(new Guid("778e144e9b324e68970470e1be9c3167")), // Civilian nightvision masterbundle override, DELETE LATER!!!!!!!!!!!!
        };
        public WiredAssetsService()
        {
            WiredAssets.Concat(_defaultAssets.ToDictionary(a => a.GUID, a => a));
            PopulateAssets();

            foreach (var asset in _defaultAssets)
            {
                if (!WiredAssets.ContainsKey(asset.GUID))
                {
                    WiredAssets.Add(asset.GUID, asset);
                    WiredLogger.Log($"Added default wired asset: {asset.Type} ({asset.GUID})");
                }
            }
        }
        private void PopulateAssets()
        {
            List<ItemAsset> items = new List<ItemAsset>();
            Assets.find(items);

            foreach (ItemAsset asset in items)
            {
                AssetParser parser = new AssetParser(asset.getFilePath());
                string[] stringstoparse = new string[] {
                    "WiredType WiringTool",
                    "WiredType RemoteTool",
                    "WiredType ManualTablet",
                    "WiredType Consumer",
                    "WiredType Supplier",
                    "WiredType Gate",
                };
                if (parser.HasAnyEntry(stringstoparse, out var foundentry))
                {
                    WiredLogger.Log($"Found wired asset: {asset.name} ({asset.GUID}) as {foundentry}");
                    switch (foundentry.Split(' ')[1])
                    {
                        default:
                            break;
                        case "WiringTool":
                            WiredAssets.Add(asset.GUID, new WiringToolAsset(asset.GUID));
                            break;
                        case "RemoteTool":
                            WiredAssets.Add(asset.GUID, new RemoteToolAsset(asset.GUID));
                            break;
                        case "ManualTablet":
                            WiredAssets.Add(asset.GUID, new ManualTabletAsset(asset.GUID));
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
                    }
                }
            }
        }

        private void PopulateGate(AssetParser parser, ItemAsset asset)
        {
            if (parser.HasEntry("WiredBuild Switch"))
            {
                WiredAssets.Add(asset.GUID, new SwitchAsset(asset.GUID, true));
            }
            else if (parser.HasEntry("WiredBuild PlayerDetector"))
            {
                WiredAssets.Add(asset.GUID,
                new PlayerDetectorAsset(
                        guid: asset.GUID,
                        radius: parser.TryGetFloat("radius", out float radius) ? radius : 3f));
            }
            else if (parser.HasEntry("WiredBuild Timer"))
            {
                WiredAssets.Add(asset.GUID,
                new TimerAsset(
                guid: asset.GUID,
                delayseconds: parser.TryGetFloat("Timer_Delay_Seconds", out float delay) ? delay : 5));
            }
            else if (parser.HasEntry("WiredBuild RemoteReceiver"))
            {
                WiredAssets.Add(asset.GUID, new RemoteReceiverAsset(asset.GUID));
            }
        }
        private void PopulateConsumer(AssetParser parser, ItemAsset asset)
        {
            var consumption = parser.TryGetFloat("Power_Consumption", out float cons) ? cons : 100f;
            if(parser.HasEntry("WiredBuild RemoteTransmitter"))
            {
                var range = parser.TryGetFloat("Transmitter_Range_Meters", out float ran) ? ran : 50f;
                WiredAssets.Add(asset.GUID, new RemoteTransmitterAsset(asset.GUID, cons, range));
                return;
            }
            WiredAssets.Add(asset.GUID, new ConsumerAsset(asset.GUID, consumption));
        }
        private void PopulateSupplier(AssetParser parser, ItemAsset asset)
        {
            var supply = parser.TryGetFloat("Power_Supply", out float supp) ? supp : 100f;
            WiredAssets.Add(asset.GUID, new SupplierAsset(asset.GUID, supply));
        }
    }
}