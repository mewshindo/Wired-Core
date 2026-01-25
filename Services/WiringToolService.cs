using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wired.Models;

namespace Wired.Services
{
    public class WiringToolService
    {
        private Resources _resources;
        public readonly Dictionary<CSteamID, Transform> SelectedNode = new Dictionary<CSteamID, Transform>();
        private readonly Dictionary<CSteamID, List<Vector3>> _selectedPath = new Dictionary<CSteamID, List<Vector3>>();

        public delegate void NodeSelectedHandler(UnturnedPlayer player, Transform nodeTransform);
        public static event NodeSelectedHandler OnNodeSelected;

        public delegate void SelectionClearRequestedHandler(UnturnedPlayer player);
        public static event SelectionClearRequestedHandler OnNodeSelectionClearRequested;

        public delegate void NodeConnectionRequestedHandler(UnturnedPlayer player, IElectricNode node1, IElectricNode node2, List<Vector3> wirepath);
        public static event NodeConnectionRequestedHandler OnNodeLinkRequested;
        public WiringToolService(Resources resources)
        {
            _resources = resources;
            UseableGun.onBulletSpawned += OnBulletSpawned;
            OnNodeSelectionClearRequested += ClearSelection;
            UseableGun.OnAimingChanged_Global += OnAimingChanged_Global;
            UseableGun.onBulletHit += onBulletHit;
        }

        private void onBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldAllow)
        {
            if (!_resources.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID) && _resources.WiredAssets[gun.equippedGunAsset.GUID].Type != WiredAssetType.WiringTool)
                return;
            shouldAllow = false;
        }
        private void OnAimingChanged_Global(UseableGun gun)
        {
            if (!_resources.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID) && _resources.WiredAssets[gun.equippedGunAsset.GUID].Type != WiredAssetType.WiringTool)
                return;

            //if(!gun.isAiming) return;

            //var list = _selectedPath[gun.player.channel.owner.playerID.steamID];
            //if (list != null && list.Count > 0)
            //{
            //    list.RemoveAt(list.Count - 1);
            //}
        }

        private void OnBulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            if (!_resources.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID) && _resources.WiredAssets[gun.equippedGunAsset.GUID].Type != WiredAssetType.WiringTool)
                return;

            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(gun.player.channel.owner.playerID.steamID);
            Raycast raycast = new Raycast(gun.player);

            var drop = raycast.GetBarricade(out _);
            if(drop != null)
            {
                TrySelectNode(player, raycast, drop);
                return;
            }
            var ground = raycast.GetPoint(range: 10);
            if(ground != null)
            {
                if (!_selectedPath.ContainsKey(player.CSteamID))
                {
                    _selectedPath[player.CSteamID] = new List<Vector3>() { ground };
                    return;
                }
                _selectedPath[player.CSteamID].Add(ground);
            }
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

        private void TrySelectNode(UnturnedPlayer player, Raycast raycast, BarricadeDrop barricade)
        {
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

            if (!SelectedNode.ContainsKey(player.CSteamID))
            {
                OnNodeSelected?.Invoke(player, barricade.model);
                return;
            }

            var node1 = SelectedNode[player.CSteamID];
            var node2 = barricade.model;

            if (node1 == node2)
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

            List<Vector3> path;
            if (_selectedPath.ContainsKey(player.CSteamID))
                path = _selectedPath[player.CSteamID];
            else
                path = new List<Vector3>();

            OnNodeLinkRequested?.Invoke(player, electricnode1, electricnode2, path);
            OnNodeSelectionClearRequested?.Invoke(player);
            ClearSelection(player);
        }
        private void ClearSelection(UnturnedPlayer player)
        {
            SelectedNode.Remove(player.CSteamID);
        }
    }
}