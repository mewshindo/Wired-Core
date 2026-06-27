using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Wired.Utilities;

namespace Wired
{
    public class PlayerEvents : MonoBehaviour
    {
        public delegate void onDequipRequested(Player player, ItemAsset asset, ref bool shouldAllow);

        public static event onDequipRequested OnDequipRequested;

        public delegate void onEquipRequested(Player player, ItemAsset asset, ref bool shouldAllow);
        public static event onEquipRequested OnEquipRequested;

        public void Awake()
        {
            Player player = this.gameObject.transform.GetComponent<Player>();
            WiredLogger.Info($"added coolevents to {player.name}");
            PlayerEquipment equipment2 = player.equipment;
            equipment2.onDequipRequested = (PlayerDequipRequestHandler)Delegate.Combine(equipment2.onDequipRequested, (PlayerDequipRequestHandler)delegate (PlayerEquipment equipment, ref bool shouldAllow)
            {
                OnDequipRequested?.Invoke(player, equipment.asset, ref shouldAllow);
            });
            equipment2.onEquipRequested = (PlayerEquipRequestHandler)Delegate.Combine(equipment2.onEquipRequested, (PlayerEquipRequestHandler)delegate (PlayerEquipment equipment, ItemJar jar, ItemAsset asset, ref bool shouldAllow)
            {
                OnEquipRequested?.Invoke(player, asset, ref shouldAllow);
            });
        }

        private void onGlassesUpdated(ushort newGlasses, byte newGlassesQuality, byte[] newGlassesState)
        {
            WiredLogger.Info($"New glasses quality: {newGlassesQuality}");
            WiredLogger.Info($"New glasses state length: {newGlassesState.Length}");
            for (int i = 0; i < newGlassesState.Length; i++)
            {
                WiredLogger.Info($"New glasses state[{i}]: {newGlassesState[i]}");
            }
        }
    }
}
