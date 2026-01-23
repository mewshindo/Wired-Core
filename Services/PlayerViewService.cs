using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System;
using UnityEngine;
using Wired.Models;
using Rocket.Core.Steam;

namespace Wired.Services
{
    public class PlayerViewService : MonoBehaviour
    {
        private Resources _resources;
        private readonly NodeConnectionsService _nodeService;
        private readonly Dictionary<CSteamID, Transform> _selectedNode;
        private readonly Dictionary<CSteamID, uint> _lookingAt;
        private readonly List<UnturnedPlayer> _playersInLinkingMode;
        public PlayerViewService(Resources resources, NodeConnectionsService ns, Dictionary<CSteamID, Transform> selectedNode)
        {
            WiringToolService.OnNodeSelected += OnNodeSelected;
            WiringToolService.OnNodeSelectionClearRequested += OnNodeSelectionCleared;
            PlayerEvents.OnDequipRequested += OnDequipRequested;
            PlayerEvents.OnEquipRequested += OnEquipRequested;
            _resources = resources;
            _nodeService = ns;
            _selectedNode = selectedNode;
            _lookingAt = new Dictionary<CSteamID, uint>();
        }

        private void OnEquipRequested(Player player, ItemAsset asset, ref bool shouldAllow)
        {
            if(_resources.WiredAssets.ContainsKey(asset.GUID) && _resources.WiredAssets[asset.GUID].Type == WiredAssetType.WiringTool)
            {
                DisplayNodes(player.channel.owner.playerID.steamID);
            }
        }

        private void OnDequipRequested(Player player, ItemAsset asset, ref bool shouldAllow)
        {
            if (_resources.WiredAssets.ContainsKey(asset.GUID) && _resources.WiredAssets[asset.GUID].Type == WiredAssetType.WiringTool)
            {
                player.ServerShowHint("", 0);
                UnturnedPlayer uplayer = UnturnedPlayer.FromPlayer(player);
                _playersInLinkingMode.Remove(uplayer);
                _lookingAt.Remove(uplayer.CSteamID);
            }
        }

        private void OnNodeSelectionCleared(UnturnedPlayer player)
        {
            foreach (Guid guid in _resources.nodeeffects)
                EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(player.CSteamID));

            player.Player.ServerShowHint("Selection cleared.", 2f);
            _playersInLinkingMode.Remove(player);
            _lookingAt.Remove(player.CSteamID);
        }

        private void OnNodeSelected(UnturnedPlayer player, Transform nodeTransform)
        {
            player.Player.ServerShowHint("Click on another thing to link them together!<br>Click anywhere else to cancel.", 5f);
            _playersInLinkingMode.Add(player);
        }

        private void Update()
        {
            foreach (var player in _playersInLinkingMode)
            {
                if (player.Player == null || !_selectedNode.ContainsKey(player.CSteamID))
                {
                    _playersInLinkingMode.Remove(player);
                    _lookingAt.Remove(player.CSteamID);
                    continue;
                }

                Raycast ray = new Raycast(player.Player);
                BarricadeDrop drop = ray.GetBarricade(out _);
                if (drop == null)
                    continue;

                var iid = drop.instanceID;
                if (_lookingAt[player.CSteamID] == iid)
                    continue;

                if(!drop.model.TryGetComponent(out IElectricNode node))
                {
                    foreach (Guid guid in _resources.previeweffects)
                        EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(player.CSteamID));
                    continue;
                }

                _lookingAt[player.CSteamID] = iid;

                foreach (Guid guid in _resources.previeweffects)
                    EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(player.CSteamID));

                EffectAsset effect;
                switch (node.GetType().ToString())
                {
                    case "SupplierNode":
                        effect = _resources.preview_power;
                        break;
                    case "SwitchNode":
                        effect = _resources.preview_gate;
                        break;
                    default:
                        effect = _resources.preview_consumer;
                        break;
                }
                TracePath(player, _selectedNode[player.CSteamID].position, drop.model.position, effect);
            }
        }

        private void DisplayNodes(CSteamID steamid)
        {
            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamid);
            if (player == null) return;

            HashSet<IElectricNode> visibleNodes = new HashSet<IElectricNode>();

            var bfinder = new BarricadeFinder(player.Position, 100f);
            foreach (BarricadeDrop drop in bfinder.GetBarricadesInRadius())
            {
                Transform t = drop.model;
                if (t == null) continue;

                if (!t.TryGetComponent(out IElectricNode node))
                    continue;

                if (!DoesOwnDrop(drop, steamid))
                    continue;

                visibleNodes.Add(node);

                if (node is ConsumerNode)
                    sendEffectCool(player, t.position, _resources.node_consumer);
                else if (node is SupplierNode)
                    sendEffectCool(player, t.position, _resources.node_power);
                else if (node is SwitchNode)
                    sendEffectCool(player, t.position, _resources.node_gate);
            }

            foreach (var connection in _nodeService.GetAllConnections())
            {
                bool isNode1Visible = visibleNodes.Contains(connection.Node1);
                bool isNode2Visible = visibleNodes.Contains(connection.Node2);

                if (!isNode1Visible && !isNode2Visible)
                    continue;

                EffectAsset pathEffect;

                if (connection.Node1 is SupplierNode || connection.Node2 is SupplierNode)
                {
                    pathEffect = _resources.path_power;
                }
                else if (connection.Node1 is SwitchNode || connection.Node2 is SwitchNode)
                {
                    pathEffect = _resources.path_gate;
                }
                else
                {
                    pathEffect = _resources.path_consumer;
                }

                Vector3 start = ((MonoBehaviour)connection.Node1).transform.position;
                Vector3 end = ((MonoBehaviour)connection.Node2).transform.position;

                TracePath(player, start, end, pathEffect);
            }

            Console.WriteLine($"Displayed nodes to {player.DisplayName}");
        }

        private void TracePath(UnturnedPlayer player, Vector3 point1, Vector3 point2, EffectAsset pathEffect)
        {
            float distance = Vector3.Distance(point1, point2);

            Vector3 direction = (point2 - point1).normalized;

            TriggerEffectParameters effect = new TriggerEffectParameters
            {
                asset = pathEffect,
                position = point1,
                relevantDistance = 64f,
                shouldReplicate = true,
                reliable = true,
                scale = new Vector3(1f, 1f, distance)
            };
            effect.SetDirection(direction);
            effect.SetRelevantPlayer(player.SteamPlayer());
            EffectManager.triggerEffect(effect);
        }

        private void sendEffectCool(UnturnedPlayer player, Vector3 dropPosition, EffectAsset asset)
        {
            TriggerEffectParameters effect = new TriggerEffectParameters
            {
                asset = asset,
                position = dropPosition,
                relevantDistance = 64f,
                shouldReplicate = true,
                reliable = true,
            };
            effect.SetDirection(Vector3.down);
            effect.SetRelevantPlayer(player.SteamPlayer());
            EffectManager.triggerEffect(effect);
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
    }
}
