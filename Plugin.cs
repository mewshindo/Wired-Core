using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Wired.Models;
using Wired.Services;

namespace Wired
{
    public class Plugin : RocketPlugin<Config>
    {
        public static Plugin Instance;

        private Resources _resources;

        private ServiceContainer _services;

        public delegate void SwitchToggled(SwitchNode sw, bool state);
        public static event SwitchToggled OnSwitchToggled;
        protected override void Load()
        {
            Instance = this;

            Harmony harmony = new Harmony("com.mew.powerShenanigans");
            harmony.PatchAll();
            foreach (MethodBase method in harmony.GetPatchedMethods())
            {
                Console.WriteLine("Patched method: " + method.DeclaringType.FullName + "." + method.Name);
            }
            Level.onLevelLoaded += OnLevelLoaded;
        }
        protected override void Unload()
        {
            Harmony harmony = new Harmony("com.mew.powerShenanigans");
            harmony.UnpatchAll("com.mew.powerShenanigans");
            Instance = null;
        }

        private void OnLevelLoaded(int lvl)
        {
            List<ItemAsset> items = new List<ItemAsset>();
            Assets.find(items);

            foreach (ItemAsset asset in items)
            {
                AssetParser parser = new AssetParser(asset.getFilePath());
                string[] stringstoparse = new string[] {
                    "WiringTool",
                    "RemoteTool",
                    "Gate",
                    "Switch",
                    "Timer",
                    "RemoteReceiver",
                    "RemoteTransmitter",
                    "ManualTablet"
                };
                if (parser.HasAnyEntry(stringstoparse, out var foundentry))
                {
                    Console.WriteLine($"Found wired asset: {asset.name} ({asset.GUID}) as {foundentry}");
                    switch (foundentry)
                    {
                        default:
                            break;
                        case "WiringTool":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.WiringTool));
                            break;
                        case "RemoteTool":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.RemoteTool));
                            break;
                        case "ManualTablet":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.ManualTablet));
                            break;
                        case "Gate":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.Switch));
                            break;
                        case "Switch":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.Switch));
                            break;
                        case "Timer":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.Timer));
                            break;
                        case "RemoteReceiver":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.RemoteReceiver));
                            break;
                        case "RemoteTransmitter":
                            _resources.WiredAssets.Add(asset.GUID, new WiredAsset(asset.GUID, WiredAssetType.RemoteTransmitter));
                            break;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(InteractableSpot), "ReceiveToggleRequest")]
        private static class Patch_InteractableSpot_ReceiveToggleRequest
        {
            private static bool Prefix(InteractableSpot __instance, ServerInvocationContext context, bool desiredPowered)
            {
                Player player = context.GetPlayer();
                Console.WriteLine(string.Format("[PowerShenanigans] ReceiveToggleRequest from player {0} desiredPowered={1}, __instance.name: {2}", player?.ToString() ?? "null", desiredPowered, __instance.name));
                if (player == null)
                {
                    return true;
                }
                if (__instance.gameObject.TryGetComponent(out SwitchNode sw))
                {
                    sw.Switch(desiredPowered);
                    OnSwitchToggled?.Invoke(sw, desiredPowered);
                    return true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InteractableFarm), "updatePlanted")]
        private static class Patch_InteractableFarm_updatePlanted
        {
            private static void Postfix(InteractableFarm __instance, uint newPlanted)
            {
                Console.WriteLine($"newPlanted: {newPlanted}\n ProviderTime: {Provider.time}");
            }
        }
    }
}