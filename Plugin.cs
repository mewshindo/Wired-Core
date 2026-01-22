using System;
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
        }
        protected override void Unload()
        {
            Harmony harmony = new Harmony("com.mew.powerShenanigans");
            harmony.UnpatchAll("com.mew.powerShenanigans");
            Instance = null;
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
                if (__instance.gameObject.GetComponent<SwitchNode>() != null)
                {
                    __instance.gameObject.GetComponent<SwitchNode>()?.Switch(desiredPowered);
                    OnSwitchToggled?.Invoke(__instance.gameObject.GetComponent<SwitchNode>(), desiredPowered);
                    return true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InteractableFire), "ReceiveToggleRequest")]
        private static class Patch_InteractableFire_ReceiveToggleRequest
        {
            private static bool Prefix(InteractableFire __instance, ServerInvocationContext context, bool desiredLit)
            {
                Console.WriteLine(string.Format("[PowerShenanigans] ReceiveToggleRequest from player {0} desiredLit={1}, __instance.name: {2}", context.GetPlayer()?.ToString() ?? "null", desiredLit, __instance.name));
                if (__instance.name == "1272")
                {
                    //__instance.gameObject.GetComponent<GateNode>()?.Toggle(desiredLit);

                }
                return true;
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