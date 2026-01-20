using System;
using System.Reflection;
using HarmonyLib;
using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;

namespace Wired
{
    public class Plugin : RocketPlugin<Config>
    {
        public static Plugin Instance;
        public Resources Resources;
        protected override void Load()
        {
            Instance = this;
            Resources = new Resources();

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
            //private static bool Prefix(InteractableSpot __instance, ServerInvocationContext context, bool desiredPowered)
            //{
            //    Player player = context.GetPlayer();
            //}
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