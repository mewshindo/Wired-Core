using System;
using System.Net;
using System.Reflection;
using HarmonyLib;
using Rocket.API;
using Rocket.API.Extensions;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using Wired.Models;
using Wired.Services;
using Wired.Utilities;
using Wired.WiredInteractables;

namespace Wired
{
    public class Plugin : RocketPlugin<Config>
    {
        public static Plugin Instance;

        private bool _levelLoaded;

        public Resources Resources;

        public ServiceContainer Services;

        public delegate void SwitchToggled(GateNode sw, bool state);
        public static event SwitchToggled OnSwitchToggled;
        public void SendSwitchToggled(GateNode sw, bool state) => OnSwitchToggled?.Invoke(sw, state);

        public delegate void TimerExpired(TimerNode timer);
        public static event TimerExpired OnTimerExpired;
        public void SendTimerExpired(TimerNode timer) => OnTimerExpired?.Invoke(timer);

        public delegate void DragItemRequested(PlayerInventory inventory, ItemAsset asset, ref bool shouldAllow);
        public static event DragItemRequested OnDragItemRequested;

        public delegate void SwapItemRequested(PlayerInventory inventory, ItemAsset item1, ItemAsset item2, ref bool shouldAllow);
        public static event SwapItemRequested OnSwapItemRequested;

        public delegate void DropItemRequested(PlayerInventory inventory, ItemAsset asset, ref bool shouldAllow);
        public static event DropItemRequested OnDropItemRequested;

        public delegate void GeneratorFuelUpdated(InteractableGenerator generator, ushort newAmount);
        public static event GeneratorFuelUpdated OnGeneratorFuelUpdated;

        public delegate void GeneratorPoweredChanged(InteractableGenerator generator, bool isPowered);
        public static event GeneratorPoweredChanged OnGeneratorPoweredChanged;

        public delegate void KeypadInteracted(Keypad keypad, Player player);
        public static event KeypadInteracted OnKeypadInteractRequested;

        public delegate void TimeOfDayUpdated(uint timeOfDay, float timefraction);
        public static event TimeOfDayUpdated OnTimeOfDayUpdated;

        protected override void Load()
        {
            Instance = this;

            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            Harmony harmony = new Harmony("com.mew.powerShenanigans");
            harmony.PatchAll();
            foreach (MethodBase method in harmony.GetPatchedMethods())
            {
                WiredLogger.Info("Patched method: " + method.DeclaringType.FullName + "." + method.Name);
            }

            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            Level.onLevelLoaded += OnLevelLoaded;
            BarricadeManager.onModifySignRequested += OnModifySignRequested;
        }

        protected override void Unload()
        {
            ServicePointManager.ServerCertificateValidationCallback -= (sender, certificate, chain, sslPolicyErrors) => true;

            Services.WiredDeltaService.Disconnect(new ConsolePlayer());

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
                WiredLogger.PluginLoaded(false);
                Instance.UnloadPlugin();
                return;
            }

            WiredLogger.PluginLoaded(true);
            _levelLoaded = true;
            Resources = new Resources();
            Services = new ServiceContainer(Resources);
        }

        private uint time = 0;
        private void Update()
        {
            if (!_levelLoaded) return;
            if (LightingManager.time == time) return;

            time = LightingManager.time;
            OnTimeOfDayUpdated?.Invoke(LightingManager.time, (float)LightingManager.time / (float)LightingManager.cycle);

            return;
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            player.Player.gameObject.TryRemoveComponent<PlayerEvents>();
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.Player.gameObject.AddComponent<PlayerEvents>();

            ITransportConnection connection = player.Player.channel.owner.transportConnection;
            EffectManager.SendUIEffect(Resources.goggles_ui, Resources.GogglesUIKey, connection, true);
        }

        private void OnModifySignRequested(CSteamID instigator, InteractableSign sign, ref string text, ref bool shouldAllow)
        {
            if (sign.gameObject.TryGetComponent(out RemoteTransmitter rt))
            {
                rt.TrySetFrequency(text, PlayerTool.getPlayer(instigator));
            }
            else if (sign.gameObject.TryGetComponent(out RemoteReceiver rr))
            {
                rr.TrySetFrequency(text, PlayerTool.getPlayer(instigator));
            }
            else if (sign.gameObject.TryGetComponent(out TimerNode timer))
            {
                shouldAllow = false;
            }
        }

        [HarmonyPatch(typeof(InteractableSpot), "ReceiveToggleRequest")]
        private static class Patch_InteractableSpot_ReceiveToggleRequest
        {
            private static bool Prefix(InteractableSpot __instance, ServerInvocationContext context, bool desiredPowered)
            {
                Player player = context.GetPlayer();
                if (player == null)
                {
                    Console.WriteLine($"ReceiveToggleRequest behaviour 1");
                    return true;
                }

                if (__instance.gameObject.TryGetComponent(out GateNode sw))
                {
                    Console.WriteLine($"ReceiveToggleRequest behaviour 2");

                    if (!sw.SwitchableByPlayer)
                    {
                        Console.WriteLine($"ReceiveToggleRequest behaviour 3");
                        if (__instance.gameObject.TryGetComponent(out Keypad kp))
                        {
                            Console.WriteLine($"ReceiveToggleRequest behaviour 4");
                            OnKeypadInteractRequested?.Invoke(kp, player);
                            return false;
                        }
                        return false;
                    }
                    Console.WriteLine($"ReceiveToggleRequest behaviour 5");
                    sw.Switch(desiredPowered);
                    return true;
                }
                Console.WriteLine($"ReceiveToggleRequest behaviour 6");
                return false;
            }
        }
        [HarmonyPatch(typeof(InteractableOxygenator), "ReceiveToggleRequest")]
        private static class Patch_InteractableOxygenator_ReceiveToggleRequest
        {
            private static bool Prefix(InteractableOxygenator __instance, ServerInvocationContext context, bool desiredPowered)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(InteractableFarm), "updatePlanted")]
        private static class Patch_InteractableFarm_updatePlanted
        {
            private static void Postfix(InteractableFarm __instance, uint newPlanted)
            {
                WiredLogger.Info($"newPlanted: {newPlanted}\n ProviderTime: {Provider.time}");
            }
        }

        [HarmonyPatch(typeof(InteractableGenerator), "askBurn")]
        public static class Property_Patch
        {
            public static void Postfix(InteractableGenerator __instance, ushort amount)
            {
                if (__instance.fuel - amount <= 0)
                {
                    OnGeneratorFuelUpdated?.Invoke(__instance, (ushort)(__instance.fuel - amount));
                }
            }
        }
        [HarmonyPatch(typeof(InteractableGenerator), "askFill")]
        public static class Property_Patch_Fill
        {
            public static void Postfix(InteractableGenerator __instance, ushort amount)
            {
                OnGeneratorFuelUpdated?.Invoke(__instance, (ushort)(__instance.fuel + amount));
            }
        }
        [HarmonyPatch(typeof(InteractableGenerator), "ReceivePowered")]
        public static class Property_Patch_ReceivePowered
        {
            public static void Postfix(InteractableGenerator __instance, bool newPowered)
            {
                OnGeneratorPoweredChanged?.Invoke(__instance, newPowered);
            }
        }

        //[HarmonyPatch(typeof(PlayerInventory), "ReceiveDragItem")]
        //private static class Patch_PlayerInventory_ReceiveDragItem
        //{
        //    private static bool Prefix(PlayerInventory __instance, byte page_0, byte x_0, byte y_0, byte page_1, byte x_1, byte y_1, byte rot_1)
        //    {
        //        if(__instance.storage.TryGetComponent(out IWiredInteractable wi))
        //        {
        //            bool shouldAllow = false;
        //            OnDragItemRequested?.Invoke(__instance, __instance.getItem(page_0, __instance.getIndex(page_0, x_0, y_0)).GetAsset(), ref shouldAllow);
        //            return shouldAllow;
        //        }
        //        return true;
        //    }
        //}
        //[HarmonyPatch(typeof(PlayerInventory), "ReceiveSwapItem")]
        //private static class Patch_PlayerInventory_ReceiveSwapItem
        //{
        //    private static bool Prefix(PlayerInventory __instance, byte page_0, byte x_0, byte y_0, byte rot_0, byte page_1, byte x_1, byte y_1, byte rot_1)
        //    {
        //        if(__instance.storage.TryGetComponent(out IWiredInteractable wi))
        //        {
        //            bool shouldAllow = false;
        //            var item1 = __instance.getItem(page_0, __instance.getIndex(page_0, x_0, y_0)).GetAsset();
        //            var item2 = __instance.getItem(page_1, __instance.getIndex(page_1, x_1, y_1)).GetAsset();
        //            OnSwapItemRequested?.Invoke(__instance, item1, item2, ref shouldAllow);
        //            return shouldAllow;
        //        }
        //        return true;
        //    }
        //}
        //[HarmonyPatch(typeof(PlayerInventory), "ReceiveDropItem")]
        //private static class Patch_PlayerInventory_ReceiveDropItem
        //{
        //    private static bool Prefix(PlayerInventory __instance, byte page, byte x, byte y)
        //    {
        //        if(__instance.storage.TryGetComponent(out IWiredInteractable wi))
        //        {
        //            bool shouldAllow = false;
        //            var item = __instance.getItem(page, __instance.getIndex(page, x, y)).GetAsset();
        //            OnDropItemRequested?.Invoke(__instance, item, ref shouldAllow);
        //            return shouldAllow;
        //        }
        //        return true;
        //    }
        //}
    }

    public enum RemoteSignalType
    {
        ON,
        OFF,
        TOGGLE
    }
}