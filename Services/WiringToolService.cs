using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Models;

namespace Wired.Services
{
    public class WiringToolService
    {
        private Resources _resources;
        private readonly Dictionary<CSteamID, Transform> _selectedNode = new Dictionary<CSteamID, Transform>();

        public delegate void NodeSelectedHandler(UnturnedPlayer player, Transform nodeTransform);
        public static event NodeSelectedHandler OnNodeSelected;

        public delegate void SelectionClearRequestedHandler(UnturnedPlayer player);
        public static event SelectionClearRequestedHandler OnNodeSelectionClearRequested;

        public delegate void NodeConnectionRequestedHandler(UnturnedPlayer player, IElectricNode node1, IElectricNode node2);
        public static event NodeConnectionRequestedHandler OnNodeLinkRequested;
        public WiringToolService(Resources resources)
        {
            _resources = resources;
            UseableGun.onBulletSpawned += OnBulletSpawned;
            OnNodeSelectionClearRequested += ClearSelection;
            OnNodeSelected += SelectNode;
        }

        private void OnBulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            if (!_resources.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID) && _resources.WiredAssets[gun.equippedGunAsset.GUID].Type != WiredAssetType.WiringTool)
                return;

            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(gun.player.channel.owner.playerID.steamID);
            Raycast raycast = new Raycast(gun.player);

            BarricadeDrop barricade = raycast.GetBarricade(out _);

            if (barricade == null)
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                return;
            }

            if (!DoesOwnDrop(barricade, player.CSteamID))
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                return;
            }

            if (!_selectedNode.ContainsKey(player.CSteamID))
            {
                OnNodeSelected?.Invoke(player, barricade.model);
                return;
            }

            var node1 = _selectedNode[player.CSteamID];
            var node2 = barricade.model;

            if(node1 == node2)
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                return;
            }

            var distance = Math.Round(Vector3.Distance(node1.position, node2.position), 1);
            if (distance > 25)
            {
                player.Player.ServerShowHint($"The components are too far apart! ({distance} > 25)", 5f);
            }

            var electricnode1 = node1.GetComponent<IElectricNode>();
            var electricnode2 = node2.GetComponent<IElectricNode>();

            if (electricnode1 == null || electricnode2 == null)
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                return;
            }

            OnNodeLinkRequested?.Invoke(player, electricnode1, electricnode2);
            ClearSelection(player);
        }


        private bool DoesOwnDrop(BarricadeDrop drop, CSteamID steamid)
        {
            var dropdata = drop.GetServersideData();
            if (dropdata.owner != 0 && dropdata.owner == (ulong)steamid)
                return true;
            if (dropdata.group != 0 && dropdata.group == (ulong)UnturnedPlayer.FromCSteamID(steamid).SteamGroupID)
                return true;
            if (dropdata.group != 0 && dropdata.group == (ulong)UnturnedPlayer.FromCSteamID(steamid).Player.quests.groupID)
                return true;
            return false;
        }


        private void ClearSelection(UnturnedPlayer player)
        {
            _selectedNode.Remove(player.CSteamID);
        }
        private void SelectNode(UnturnedPlayer player, Transform nodeTransform)
        {
            _selectedNode[player.CSteamID] = nodeTransform;
        }
    }
}