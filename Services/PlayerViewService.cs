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
        private NodeConnectionsService _ncs;
        private Dictionary<CSteamID, Transform> _selectedNode;
        private Dictionary<CSteamID, uint> _lookingAt = new Dictionary<CSteamID, uint>();
        private readonly List<UnturnedPlayer> _playersInLinkingMode = new List<UnturnedPlayer>();
        public void Init(Resources resources, NodeConnectionsService ncs, Dictionary<CSteamID, Transform> selectedNode)
        {
            WiringToolService.OnNodeSelected += OnNodeSelected;
            WiringToolService.OnNodeSelectionClearRequested += OnNodeSelectionCleared;
            NodeConnectionsService.OnNodeConnected += OnNodeConnected;
            NodeConnectionsService.OnNodeDisconnected += OnNodeDisconnected;
            PlayerEvents.OnDequipRequested += OnDequipRequested;
            PlayerEvents.OnEquipRequested += OnEquipRequested;
            PlayerEquipment.OnUseableChanged_Global += PlayerEquipment_OnUseableChanged_Global;
            _resources = resources;
            _ncs = ncs;
            _selectedNode = selectedNode;
        }

        private void OnNodeDisconnected(UnturnedPlayer player, NodeConnection connection)
        {
            UpdateNodesView(player.CSteamID);
            ClearPreviewView(player.CSteamID);
            _lookingAt[player.CSteamID] = 0;
            _playersInLinkingMode.Remove(player);
        }

        private void OnNodeConnected(UnturnedPlayer player, NodeConnection connection)
        {
            UpdateNodesView(player.CSteamID);
            ClearPreviewView(player.CSteamID);
            _lookingAt[player.CSteamID] = 0;
            _playersInLinkingMode.Remove(player);
        }

        private void PlayerEquipment_OnUseableChanged_Global(PlayerEquipment obj)
        {
            var asset = obj.asset;
            if (asset == null)
            {
                return;
            }
            if (_resources.WiredAssets.ContainsKey(asset.GUID) && _resources.WiredAssets[asset.GUID].Type == WiredAssetType.WiringTool)
            {
                UpdateNodesView(obj.player.channel.owner.playerID.steamID);
            }
        }

        private void OnEquipRequested(Player player, ItemAsset asset, ref bool shouldAllow)
        {
            if(_resources.WiredAssets.ContainsKey(asset.GUID) && _resources.WiredAssets[asset.GUID].Type == WiredAssetType.WiringTool)
            {
                UpdateNodesView(player.channel.owner.playerID.steamID);
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
                _selectedNode.Remove(player.channel.owner.playerID.steamID);
                ClearNodeView(player.channel.owner.playerID.steamID);
                ClearPreviewView(player.channel.owner.playerID.steamID);
            }
        }

        private void OnNodeSelectionCleared(UnturnedPlayer player)
        {
            player.Player.ServerShowHint("Selection cleared.", 2f);
            _playersInLinkingMode.Remove(player);
            _lookingAt.Remove(player.CSteamID);
        }

        private void OnNodeSelected(UnturnedPlayer player, Transform nodeTransform)
        {
            player.Player.ServerShowHint("Click on another <b>thing</b> to link them together!<br>Click anywhere else to cancel.", 60f);
            _selectedNode[player.CSteamID] = nodeTransform;
            _playersInLinkingMode.Add(player);
            _lookingAt[player.CSteamID] = 0;
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
                {
                    _lookingAt[player.CSteamID] = 0;
                    ClearPreviewView(player.CSteamID);
                    continue;
                }

                var lookingatID = drop.instanceID;
                if(drop.model == _selectedNode[player.CSteamID]) // If the drop they lookign at is the one they have selected
                {
                    _lookingAt[player.CSteamID] = lookingatID;
                    ClearPreviewView(player.CSteamID);
                    continue;
                }
                if (_lookingAt[player.CSteamID] == lookingatID) // If the drop they looking at hasn't changed since last update()
                {
                    continue;
                }
                if(!drop.model.TryGetComponent(out IElectricNode node)) // If the barricade is not a Wired component
                {
                    _lookingAt[player.CSteamID] = lookingatID;
                    ClearPreviewView(player.CSteamID);
                    continue;
                }
                if(_ncs.GetConnection(node, _selectedNode[player.CSteamID].GetComponent<IElectricNode>()) != null) // If looking at the existing connection
                {
                    _lookingAt[player.CSteamID] = lookingatID;
                    ClearPreviewView(player.CSteamID);
                    continue;
                }

                _lookingAt[player.CSteamID] = lookingatID;

                foreach (Guid guid in _resources.previeweffects)
                    EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(player.CSteamID));

                EffectAsset effect;
                var node1type = GetComponent<IElectricNode>();
                var node2type = GetComponent<IElectricNode>();

                if (node1type is SupplierNode || node2type is SupplierNode)
                    effect = _resources.preview_power;
                else if (node2type is SwitchNode || node2type is SwitchNode)
                    effect = _resources.preview_gate;
                else if (node2type is TimerNode || node2type is TimerNode)
                    effect = _resources.preview_timer;
                else
                    effect = _resources.preview_consumer;


                TracePath(player, _selectedNode[player.CSteamID].position, drop.model.position, effect);
            }
        }

        private void UpdateNodesView(CSteamID steamid)
        {
            foreach (Guid guid in _resources.nodeeffects)
                EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(steamid));

            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamid);
            if (player == null) return;

            HashSet<IElectricNode> visibleNodes = new HashSet<IElectricNode>();

            var bfinder = new BarricadeFinder(player.Position);
            foreach (BarricadeDrop drop in bfinder.GetBarricadesInRadius(100f))
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
                else if (node is TimerNode)
                    sendEffectCool(player, t.position, _resources.node_timer);
            }

            foreach (var connection in _ncs.GetAllConnections())
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
        }
        private void ClearNodeView(CSteamID steamid)
        {
            foreach (Guid guid in _resources.nodeeffects)
                EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(steamid));
        }
        private void ClearPreviewView(CSteamID steamid)
        {
            foreach (Guid guid in _resources.previeweffects)
                EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(steamid));
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
