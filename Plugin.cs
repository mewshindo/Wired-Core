using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Rocket.API.Extensions;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using SDG.Unturned;
using Wired.Models;
using Wired.Services;

namespace Wired
{
    public class Plugin : RocketPlugin<Config>
    {
        public static Plugin Instance;

        public Resources Resources;

        private ServiceContainer _services;

        public delegate void SwitchToggled(SwitchNode sw, bool state);
        public static event SwitchToggled OnSwitchToggled;
        public void SendSwitchToggled(SwitchNode sw, bool state) => OnSwitchToggled?.Invoke(sw, state);

        public delegate void TimerExpired(TimerNode timer);
        public static event TimerExpired OnTimerExpired;
        public void SendTimerExpired(TimerNode timer) => OnTimerExpired?.Invoke(timer);

        protected override void Load()
        {
            Instance = this;

            Harmony harmony = new Harmony("com.mew.powerShenanigans");
            harmony.PatchAll();
            foreach (MethodBase method in harmony.GetPatchedMethods())
            {
                Console.WriteLine("Patched method: " + method.DeclaringType.FullName + "." + method.Name);
            }

            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Level.onLevelLoaded += OnLevelLoaded;
        }

        protected override void Unload()
        {
            Harmony harmony = new Harmony("com.mew.powerShenanigans");
            harmony.UnpatchAll("com.mew.powerShenanigans");

            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            Level.onLevelLoaded -= OnLevelLoaded;

            Instance = null;
        }
        private void OnLevelLoaded(int lvl)
        {
            if (!Provider.getServerWorkshopFileIDs().Contains(3583223837))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("##########################################################");
                Console.WriteLine("##########################################################");
                Console.WriteLine("");
                Console.WriteLine("                 WIRED IS NOT INSTALLED");
                Console.WriteLine("    Add 3583223837 to your WorkshopDownloadConfig.json");
                Console.WriteLine("");
                Console.WriteLine("##########################################################");
                Console.WriteLine("##########################################################");
                Console.ResetColor();

                Instance.UnloadPlugin();
                return;
            }


            Resources = new Resources();
            _services = new ServiceContainer(Resources);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("           _              _ ");
            Console.WriteLine("          (_)            | |");
            Console.WriteLine(" __      ___ _ __ ___  __| |");
            Console.WriteLine(" \\ \\ /\\ / / | '__/ _ \\/ _` |        Wired has loaded succesfully!");
            Console.WriteLine("  \\ V  V /| | | |  __/ (_| |");
            Console.WriteLine("   \\_/\\_/ |_|_|  \\___|\\__,_|\n");
            Console.ResetColor();
        }


        private void OnPlayerDisconnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            player.Player.gameObject.TryRemoveComponent<PlayerEvents>();
        }

        private void OnPlayerConnected(Rocket.Unturned.Player.UnturnedPlayer player)
        {
            player.Player.gameObject.AddComponent<PlayerEvents>();
        }

        [HarmonyPatch(typeof(InteractableSpot), "ReceiveToggleRequest")]
        private static class Patch_InteractableSpot_ReceiveToggleRequest
        {
            private static bool Prefix(InteractableSpot __instance, ServerInvocationContext context, bool desiredPowered)
            {
                Player player = context.GetPlayer();
                if (player == null)
                {
                    return true;
                }
                if (__instance.gameObject.TryGetComponent(out SwitchNode sw))
                {
                    sw.Switch(desiredPowered);
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