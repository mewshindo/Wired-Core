using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wired.Models;
using Wired.WiredAssets;

namespace Wired.Services
{
    public class WiringToolService
    {
        private readonly WiredAssetsService _assets;
        private readonly NodeConnectionsService _ncs;
        public readonly Dictionary<CSteamID, Transform> SelectedNode = [];
        private readonly Dictionary<CSteamID, List<Vector3>> _selectedPath = [];

        public delegate void NodeSelectedHandler(UnturnedPlayer player, Transform nodeTransform);
        public static event NodeSelectedHandler OnNodeSelected;

        public delegate void SelectionClearRequestedHandler(UnturnedPlayer player, bool clear = false);
        public static event SelectionClearRequestedHandler OnNodeSelectionClearRequested;

        public delegate void NodeConnectionRequestedHandler(UnturnedPlayer player, IElectricNode node1, IElectricNode node2, List<Vector3> wirepath);
        public static event NodeConnectionRequestedHandler OnNodeLinkRequested;
        public WiringToolService(WiredAssetsService assets, NodeConnectionsService ncs)
        {
            _assets = assets;
            _ncs = ncs;
            UseableGun.onBulletSpawned += OnBulletSpawned;
            OnNodeSelectionClearRequested += ClearSelection;
            UseableGun.OnAimingChanged_Global += OnAimingChanged_Global;
            UseableGun.onBulletHit += onBulletHit;
        }

        private void onBulletHit(UseableGun gun, BulletInfo bullet, InputInfo hit, ref bool shouldAllow)
        {
            if (!_assets.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID))
                return;
            if (_assets.WiredAssets[gun.equippedGunAsset.GUID] is not WiringToolAsset)
                return;
            shouldAllow = false;
        }
        private void OnAimingChanged_Global(UseableGun gun)
        {
            if (!_assets.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID))
                return;
            if (_assets.WiredAssets[gun.equippedGunAsset.GUID] is not WiringToolAsset)
                return;

            if (!gun.isAiming) return;

            var uplayer = UnturnedPlayer.FromPlayer(gun.player);
            OnNodeSelectionClearRequested?.Invoke(uplayer);
        }

        private void OnBulletSpawned(UseableGun gun, BulletInfo bullet)
        {
            if (!_assets.WiredAssets.ContainsKey(gun.equippedGunAsset.GUID))
                return;
            if (_assets.WiredAssets[gun.equippedGunAsset.GUID] is not WiringToolAsset)
                return;

            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(gun.player.channel.owner.playerID.steamID);
            Raycast raycast = new(gun.player, 16);

            var drop = raycast.GetBarricade(out _, out _, out LogicGateSubnode lgs);
            if(drop != null)
            {
                TrySelectNode(player, raycast, drop, lgs);
                return;
            }
        }

        private bool DoesOwnDrop(BarricadeDrop drop, CSteamID steamid)
        {
            return true;
            var dropdata = drop.GetServersideData();
            if (dropdata.owner != 0 && dropdata.owner == (ulong)steamid)
                return true;
            if (dropdata.group != 0 && dropdata.group == (ulong)UnturnedPlayer.FromCSteamID(steamid).SteamGroupID)
                return true;
            if (dropdata.group != 0 && dropdata.group == (ulong)UnturnedPlayer.FromCSteamID(steamid).Player.quests.groupID)
                return true;
            return false;
        }

        private void TrySelectNode(UnturnedPlayer player, Raycast raycast, BarricadeDrop barricade, LogicGateSubnode lgs)
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
                if(lgs != null)
                {
                    OnNodeSelected?.Invoke(player, lgs.transform);
                    return;
                }
                else
                {
                    OnNodeSelected?.Invoke(player, barricade.model);
                    return;
                }
            }

            var node1 = SelectedNode[player.CSteamID];

            Transform node2;
            if (lgs != null)
                node2 = lgs.transform;
            else
                node2 = barricade.model;

            if (node1 == node2)
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                return;
            }

            var distance = Math.Round(Vector3.Distance(node1.position, node2.position), 1);
            if (distance > 10)
            {
                player.Player.ServerShowHint($"The components are too far apart! ({distance} > 10)", 5f);
                return;
            }

            var electricnode1 = node1.GetComponent<IElectricNode>();
            var electricnode2 = node2.GetComponent<IElectricNode>();

            if (electricnode1 == null || electricnode2 == null)
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                return;
            }

            if (electricnode1 is LogicGateSubnode n)
            {
                if (n.IsBusy)
                {
                    if(!_ncs.IsConnected(electricnode1, electricnode2))
                    {
                        OnNodeSelectionClearRequested?.Invoke(player);
                        player.Player.ServerShowHint($"You can't connect multiple nodes to a logic gate input!", 3f);
                        return;
                    }
                }
            }
            if (electricnode2 is LogicGateSubnode n2)
            {
                if (n2.IsBusy)
                {
                    if (!_ncs.IsConnected(electricnode1, electricnode2))
                    {
                        OnNodeSelectionClearRequested?.Invoke(player);
                        player.Player.ServerShowHint($"You can't connect multiple nodes to a logic gate input!", 3f);
                        return;
                    }
                }
            }
            if (electricnode1 is LogicGateSubnode && electricnode2 is LogicGateSubnode)
            {
                OnNodeSelectionClearRequested?.Invoke(player);
                player.Player.ServerShowHint($"You can't connect logic gate inputs together!", 3f);
                return;
            }

            List<Vector3> path;
            if (_selectedPath.ContainsKey(player.CSteamID))
                path = _selectedPath[player.CSteamID];
            else
                path = [];

            OnNodeLinkRequested?.Invoke(player, electricnode1, electricnode2, path);
            OnNodeSelectionClearRequested?.Invoke(player, false);
        }
        private void ClearSelection(UnturnedPlayer player, bool clear)
        {
            SelectedNode.Remove(player.CSteamID);
        }
    }
}