using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System;
using UnityEngine;
using Wired.Models;
using Rocket.Core.Steam;
using Wired.WiredAssets;
using Wired.Utilities;
using System.Linq;
using System.Reflection;
using Rocket.Unturned.Commands;
using Rocket.Unturned;
using Wired.WiredInteractables;
using Rocket.Core.Assets;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace Wired.Services;

public class PlayerViewService : MonoBehaviour
{
    private WiredAssetsService _assets;
    private Resources _resources;
    private NodeConnectionsService _ncs;
    private Dictionary<CSteamID, Transform> _selectedNode;
    private readonly Dictionary<CSteamID, uint> _lookingAt = [];
    private readonly HashSet<UnturnedPlayer> _playersInLinkingMode = [];
    private readonly HashSet<UnturnedPlayer> _playersWithGogglesOn = [];
    public void Init(WiredAssetsService assets, Resources resources, NodeConnectionsService ncs, Dictionary<CSteamID, Transform> selectedNode)
    {
        WiringToolService.OnNodeSelected += OnNodeSelected;
        WiringToolService.OnNodeSelectionClearRequested += OnNodeSelectionCleared;
        NodeConnectionsService.OnNodeConnected += OnNodeConnected;
        NodeConnectionsService.OnNodeDisconnected += OnNodeDisconnected;
        PlayerEquipment.OnUseableChanged_Global += PlayerEquipment_OnUseableChanged_Global;
        PlayerClothing.OnGlassesChanged_Global += OnGlassesChanged_Global;
        PlayerEquipment.OnUseableChanged_Global += OnUseableChanged_Global;
        BarricadeManager.onTransformRequested += OnBarricadeMoveRequested;


        U.Events.OnPlayerConnected += OnPlayerConnected;
        
        _assets = assets;
        _resources = resources;
        _ncs = ncs;
        _selectedNode = selectedNode;
    }

    private void OnBarricadeMoveRequested(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
    {
        Console.WriteLine($"Barricade move requetsed to {point}");
        StartCoroutine(BarricadeMovedCoroutine());
    }
    private IEnumerator BarricadeMovedCoroutine()
    {
        yield return new WaitForEndOfFrame();
        UpdateWires();
        foreach (var steamplayer in Provider.clients)
        {
            var equipment = UnturnedPlayer.FromSteamPlayer(steamplayer).Player.equipment.asset;
            if (equipment == null)
                continue;
            if (_assets.WiredAssets.ContainsKey(equipment.GUID) && _assets.WiredAssets[equipment.GUID] is WiringToolAsset)
                UpdateNodesView(steamplayer.playerID.steamID);
        }
    }

    private void OnUseableChanged_Global(PlayerEquipment obj)
    {
        var player = obj.player;
        var asset = obj.asset;
        if(asset != null)
        {
            if (_assets.WiredAssets.ContainsKey(asset.GUID) && _assets.WiredAssets[asset.GUID] is WiringToolAsset)
            {
                UpdateNodesView(player.channel.owner.playerID.steamID);
            }
            else
            {
                player.ServerShowHint("", 0);
                UnturnedPlayer uplayer = UnturnedPlayer.FromPlayer(player);
                _playersInLinkingMode.Remove(uplayer);
                _lookingAt.Remove(uplayer.CSteamID);
                _selectedNode.Remove(player.channel.owner.playerID.steamID);
                ClearNodeView(player.channel.owner.playerID.steamID);
                ClearPreviewView(player.channel.owner.playerID.steamID);
                ClearSelectedView(player.channel.owner.playerID.steamID);
                return;
            }
        }
        else
        {
            player.ServerShowHint("", 0);
            UnturnedPlayer uplayer = UnturnedPlayer.FromPlayer(player);
            _playersInLinkingMode.Remove(uplayer);
            _lookingAt.Remove(uplayer.CSteamID);
            _selectedNode.Remove(player.channel.owner.playerID.steamID);
            ClearNodeView(player.channel.owner.playerID.steamID);
            ClearPreviewView(player.channel.owner.playerID.steamID);
            ClearSelectedView(player.channel.owner.playerID.steamID);
            return;
        }
    }

    private void OnPlayerConnected(UnturnedPlayer player)
    {
        int i = 0;

        foreach (var con in _ncs.GetAllConnections())
        {
            i++;

            EffectAsset wire = _resources.wire_8m;
            float scalemodifier = 1f / 8f;
            var distance = Vector3.Distance(con.Node1.WireConnectPoint.position, con.Node2.WireConnectPoint.position);
            if (distance <= 10 && distance > 6)
            {
                wire = _resources.wire_8m;
                scalemodifier = 1f / 8f;
            }
            else if (distance <= 6 && distance > 4)
            {
                wire = _resources.wire_6m;
                scalemodifier = 1f / 6f;
            }
            else if (distance <= 4 && distance > 2)
            {
                wire = _resources.wire_4m;
                scalemodifier = 1f / 4f;
            }
            else
            {
                wire = _resources.wire_2m;
                scalemodifier = 1f / 2f;
            }

            Vector3 direction = (con.Node2.WireConnectPoint.position - con.Node1.WireConnectPoint.position).normalized;

            TriggerEffectParameters effect = new()
            {
                asset = wire,
                position = con.Node1.WireConnectPoint.position,
                relevantDistance = 64f,
                shouldReplicate = true,
                reliable = true,
                scale = new Vector3(1f, 1f, distance * scalemodifier)
            };
            effect.SetDirection(direction);
            effect.SetRelevantPlayer(player.Player);
            EffectManager.triggerEffect(effect);
        }
        WiredLogger.Info($"Displayed {i} wires.");
    }

    private void OnGlassesChanged_Global(PlayerClothing obj)
    {
        WiredLogger.Info($"Player {obj.player.channel.owner.playerID.steamID} changed glasses.");
        if (_playersWithGogglesOn.Contains(UnturnedPlayer.FromPlayer(obj.player)))
        {
            if(obj.glassesAsset == null)
            {
                _playersWithGogglesOn.Remove(UnturnedPlayer.FromPlayer(obj.player));
                ClearGogglesView(obj.player.channel.owner.playerID.steamID);
                return;
            }
            if(!_assets.WiredAssets.ContainsKey(obj.glassesAsset.GUID) || _assets.WiredAssets[obj.glassesAsset.GUID] is not EngineerGogglesAsset)
            {
                _playersWithGogglesOn.Remove(UnturnedPlayer.FromPlayer(obj.player));
                ClearGogglesView(obj.player.channel.owner.playerID.steamID);
                return;
            }
        }
        if(obj.glassesAsset == null)
        {
            return;
        }
        if (_assets.WiredAssets.ContainsKey(obj.glassesAsset.GUID) && (_assets.WiredAssets[obj.glassesAsset.GUID] is EngineerGogglesAsset))
        {
            _playersWithGogglesOn.Add(UnturnedPlayer.FromPlayer(obj.player));
            WiredLogger.Info($"Player {obj.player.channel.owner.playerID.steamID} has Wired Engineer Goggles on.");
        }
    }

    private void OnNodeDisconnected(UnturnedPlayer player, NodeConnection connection)
    {
        UpdateWires();
        if (player == null) return;
        UpdateNodesView(player.CSteamID);

        foreach (var steamplayer in Provider.clients)
        {
            if (steamplayer.playerID.steamID == player.CSteamID) continue;
            var equipment = UnturnedPlayer.FromSteamPlayer(steamplayer).Player.equipment.asset;
            if (equipment == null)
                continue;
            if (_assets.WiredAssets.ContainsKey(equipment.GUID) && _assets.WiredAssets[equipment.GUID] is WiringToolAsset)
                UpdateNodesView(steamplayer.playerID.steamID);

        }

        ClearPreviewView(player.CSteamID);
        _lookingAt[player.CSteamID] = 0;
        _playersInLinkingMode.Remove(player);
    }

    private void OnNodeConnected(UnturnedPlayer player, NodeConnection connection)
    {
        UpdateNodesView(player.CSteamID);
        UpdateWires();

        foreach (var steamplayer in Provider.clients)
        {
            if (steamplayer.playerID.steamID == player.CSteamID) continue;
            var equipment = UnturnedPlayer.FromSteamPlayer(steamplayer).Player.equipment.asset;
            if (equipment == null)
                continue;
            if (_assets.WiredAssets.ContainsKey(equipment.GUID) && _assets.WiredAssets[equipment.GUID] is WiringToolAsset)
                UpdateNodesView(steamplayer.playerID.steamID);

        }

        ClearPreviewView(player.CSteamID);
        ClearSelectedView(player.CSteamID);
        _lookingAt[player.CSteamID] = 0;
        _playersInLinkingMode.Remove(player);
    }

    private void PlayerEquipment_OnUseableChanged_Global(PlayerEquipment obj)
    {
        var asset = obj.asset;
        if (asset == null)
            return;


        if (_assets.WiredAssets.ContainsKey(asset.GUID) && _assets.WiredAssets[asset.GUID] is WiringToolAsset)
        {
            UpdateNodesView(obj.player.channel.owner.playerID.steamID);
        }
    }
    private void OnNodeSelectionCleared(UnturnedPlayer player)
    {
        player.Player.ServerShowHint("Selection cleared.", 2f);
        _playersInLinkingMode.Remove(player);
        _lookingAt.Remove(player.CSteamID);
        ClearPreviewView(player.CSteamID);
        ClearSelectedView(player.CSteamID);
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
        foreach (var steamplayer in Provider.clients)
        {
            var player = UnturnedPlayer.FromSteamPlayer(steamplayer);

            bool holdingWiringTool = false;
            if (player.Player.equipment.asset != null)
            {
                if (_assets.WiredAssets.ContainsKey(player.Player.equipment.asset.GUID) && _assets.WiredAssets[player.Player.equipment.asset.GUID] is WiringToolAsset)
                {
                    holdingWiringTool = true;
                }
            }

            if (!_playersWithGogglesOn.Contains(player) && !holdingWiringTool)
                continue;

            Raycast ray = new(player.Player, 16);
            BarricadeDrop drop = ray.GetBarricade(out _, out float distance, out LogicGateSubnode lgs);
            
            if (drop == null)
            {
                _lookingAt[player.CSteamID] = 0;
                ClearPreviewView(player.CSteamID);
                ClearGogglesView(player.CSteamID);
                ClearSelectedView(player.CSteamID);
                continue;
            }
            if (!DoesOwnDrop(drop, player.CSteamID))
            {
                _lookingAt[player.CSteamID] = 0;
                ClearPreviewView(player.CSteamID);
                ClearGogglesView(player.CSteamID);
                ClearSelectedView(player.CSteamID);
                continue;
            }
            if(distance > 4)
            {
                ClearGogglesView(player.CSteamID);
            }
            if (_playersWithGogglesOn.Contains(player) && distance <=4)
            {
                UpdateGogglesView(player.CSteamID, drop);
            }
            var lookingatID = drop.instanceID;
            if (!drop.model.TryGetComponent(out IElectricNode node)) // If the barricade is not a Wired component
            {
                _lookingAt[player.CSteamID] = lookingatID;
                ClearPreviewView(player.CSteamID);
                ClearSelectedView(player.CSteamID);
                ClearGogglesView(player.CSteamID);
                continue;
            }
            if(lgs == null && holdingWiringTool)
            {
                ClearSelectedView(player.CSteamID);
                switch (node)
                {
                    case GateNode:
                        sendEffectCool(player, drop.model.position, _resources.node_gate_selected);
                        break;
                    case TimerNode:
                        sendEffectCool(player, drop.model.position, _resources.node_gate_selected);
                        break;
                    case LogicGateSubnode:
                        sendEffectCool(player, drop.model.position, _resources.node_subnode_selected);
                        break;
                    case ConsumerNode:
                        sendEffectCool(player, drop.model.position, _resources.node_consumer_selected);
                        break;
                    case SupplierNode:
                        sendEffectCool(player, drop.model.position, _resources.node_power_selected);
                        break;
                    default:
                        break;
                }
            }
            else if (holdingWiringTool)
            {
                ClearSelectedView(player.CSteamID);
                sendEffectCool(player, lgs.transform.position, _resources.node_subnode_selected);
            }
            
            if (player.Player == null || !_selectedNode.ContainsKey(player.CSteamID))
            {
                _playersInLinkingMode.Remove(player);
                _lookingAt.Remove(player.CSteamID);
                continue;
            }

            if (drop.model == _selectedNode[player.CSteamID]) // If the drop they lookign at is the one they have selected
            {
                _lookingAt[player.CSteamID] = lookingatID;
                ClearPreviewView(player.CSteamID);
                continue;
            }
            if (_lookingAt[player.CSteamID] == lookingatID) // If the drop they looking at hasn't changed since last update()
            {
                continue;
            }


            if (_playersInLinkingMode.Contains(player))
            {
                if (_ncs.GetConnection(node, _selectedNode[player.CSteamID].GetComponent<IElectricNode>()) != null) // If looking at the existing connection
                {
                    _lookingAt[player.CSteamID] = lookingatID;
                    ClearPreviewView(player.CSteamID);
                    continue;
                }

                _lookingAt[player.CSteamID] = lookingatID;

                foreach (Guid guid in _resources.previeweffects)
                    EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(player.CSteamID));

                EffectAsset effect;
                var node1type = _selectedNode[player.CSteamID].GetComponent<IElectricNode>();
                var node2type = node;

                if (node1type is SupplierNode || node2type is SupplierNode)
                    effect = _resources.preview_power;
                else if (node1type is GateNode || node2type is GateNode)
                    effect = _resources.preview_gate;
                else if (node1type is TimerNode || node2type is TimerNode)
                    effect = _resources.preview_gate;
                else if (node1type is LogicGateSubnode || node2type is LogicGateSubnode)
                    effect = _resources.preview_subnode;
                else
                    effect = _resources.preview_consumer;

                switch (node2type)
                {
                    case GateNode:
                        sendEffectCool(player, drop.model.position, _resources.node_gate_selected);
                        break;
                    case TimerNode:
                        sendEffectCool(player, drop.model.position, _resources.node_gate_selected);
                        break;
                    case LogicGateSubnode:
                        sendEffectCool(player, drop.model.position, _resources.node_subnode_selected);
                        break;
                    case ConsumerNode:
                        sendEffectCool(player, drop.model.position, _resources.node_consumer_selected);
                        break;
                    case SupplierNode:
                        sendEffectCool(player, drop.model.position, _resources.node_power_selected);
                        break;
                    default:
                        break;
                }

                TracePath(player, _selectedNode[player.CSteamID].position, drop.model.position, effect);
            }
        }
    }

    private void UpdateNodesView(CSteamID steamid)
    {
        foreach (Guid guid in _resources.nodeeffects)
            EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(steamid));

        UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamid);
        if (player == null) return;

        HashSet<IElectricNode> visibleNodes = [];

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
            else if (node is GateNode gn)
            {
                sendEffectCool(player, t.position, _resources.node_gate);
                if(gn.TryGetComponent(out LogicGate lg))
                {
                    sendEffectCool(player, lg.Input0Position, _resources.node_subnode);
                    if(lg.Type != LogicGateType.NOT)
                        sendEffectCool(player, lg.Input1Position, _resources.node_subnode);
                }
            }

            else if (node is TimerNode)
                sendEffectCool(player, t.position, _resources.node_gate);
        }

        foreach (var connection in _ncs.GetAllConnections())
        {
            bool isNode1Visible = visibleNodes.Contains(connection.Node1);
            bool isNode2Visible = visibleNodes.Contains(connection.Node2);

            if (!isNode1Visible && !isNode2Visible)
                continue;
            if (!DoesOwnDrop(connection.Node1.barricade, player.CSteamID) || !DoesOwnDrop(connection.Node2.barricade, player.CSteamID))
                continue;

            EffectAsset pathEffect;

            if (connection.Node1 is SupplierNode || connection.Node2 is SupplierNode)
            {
                pathEffect = _resources.path_power;
            }
            else if (connection.Node1 is GateNode || connection.Node2 is GateNode)
            {
                pathEffect = _resources.path_gate;
            }
            else if (connection.Node1 is TimerNode || connection.Node2 is TimerNode)
            {
                pathEffect = _resources.path_gate;
            }
            else if (connection.Node1 is LogicGateSubnode || connection.Node2 is LogicGateSubnode)
            {
                pathEffect = _resources.path_subnode;
            }
            else
            {
                pathEffect = _resources.path_consumer;
            }

            Vector3 start = ((MonoBehaviour)connection.Node1).transform.position;
            Vector3 end = ((MonoBehaviour)connection.Node2).transform.position;

            TracePath(player, start, end, pathEffect);
        }
        if (_selectedNode.ContainsKey(player.CSteamID))
        {
            var selectedNode = _selectedNode[player.CSteamID];
            if (!selectedNode.TryGetComponent(out IElectricNode node)) return;

            switch (node)
            {
                case ConsumerNode:
                    sendEffectCool(player, selectedNode.position, _resources.node_consumer_selected);
                    break;
                case GateNode:
                    sendEffectCool(player, selectedNode.position, _resources.node_gate_selected);
                    break;
                case SupplierNode:
                    sendEffectCool(player, selectedNode.position, _resources.node_power_selected);
                    break;
                case TimerNode:
                    sendEffectCool(player, selectedNode.position, _resources.node_gate_selected);
                    break;
                case LogicGateSubnode:
                    sendEffectCool(player, selectedNode.position, _resources.node_subnode_selected);
                    break;
                default:
                    break;
            }
        }
    }

    private void UpdateWires()
    {
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_2m.GUID);
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_4m.GUID);
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_6m.GUID);
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_8m.GUID);

        int i = 0;

        foreach(var con in _ncs.GetAllConnections())
        {
            i++;

            EffectAsset wire = _resources.wire_8m;
            float scalemodifier = 1f/8f;
            var distance = Vector3.Distance(con.Node1.WireConnectPoint.position, con.Node2.WireConnectPoint.position);
            if (distance <= 0.5) continue;

            if (distance <= 10 && distance > 6)
            {
                wire = _resources.wire_8m;
                scalemodifier = 1f / 8f;
            }
            else if (distance <= 6 && distance > 4)
            {
                wire = _resources.wire_6m;
                scalemodifier = 1f / 6f;
            }
            else if (distance <= 4 && distance > 2)
            {
                wire = _resources.wire_4m;
                scalemodifier = 1f / 4f;
            }
            else
            {
                wire = _resources.wire_2m;
                scalemodifier = 1f / 2f;
            }

            Vector3 direction = (con.Node2.WireConnectPoint.position - con.Node1.WireConnectPoint.position).normalized;

            TriggerEffectParameters effect = new()
            {
                asset = wire,
                position = con.Node1.WireConnectPoint.position,
                relevantDistance = 4096,
                shouldReplicate = true,
                reliable = true,
                scale = new Vector3(1f, 1f, distance * scalemodifier)
            };
            effect.SetDirection(direction);
            EffectManager.triggerEffect(effect);
        }
        WiredLogger.Info($"Displayed {i} wires.");
    }

    private void UpdateGogglesView(CSteamID steamid, BarricadeDrop drop)
    {
        if(!drop.model.TryGetComponent(out IElectricNode node))
        {
            ClearGogglesView(steamid);
            return;
        }

        switch (node)
        {
            case SupplierNode sup:
                {
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Consumer", false);
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Switch", false);


                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Name_Supplier", $"{drop.asset.FriendlyName}");
                    if(sup.TryGetComponent(out Battery battery))
                        EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Stat_Supplier_Supply", $"{Math.Round(battery.Charge, 1)}; {Math.Round(sup.Supply, 1)}");
                    else    
                        EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Stat_Supplier_Supply", $"{Math.Round(sup.Supply, 1)}pu");
                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Stat_Supplier_Powered", sup.Supply > 0f ? "Yes" : "No");
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Supplier", true);
                }
                break;
            case ConsumerNode cons:
                {
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Supplier", false);
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Switch", false);


                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Name_Consumer", $"{drop.asset.FriendlyName}");
                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Stat_Consumer_Consumption", $"{cons.Consumption}pu");
                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Stat_Consumer_Powered", cons.IsPowered ? "Yes" : "No");
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Consumer", true);
                }
                break;
            case GateNode sw:
                {
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Supplier", false);
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Consumer", false);

                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Name_Switch", $"{drop.asset.FriendlyName}");
                    EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Stat_Switch_IsOn", sw.AllowPowerThrough ? "Yes" : "No");
                    EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Box_Switch", true);
                }
                break;
            default:
                break;
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
    private void ClearSelectedView(CSteamID steamid)
    {
        foreach (Guid guid in _resources.selectedeffects)
            EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(steamid));
    }
    private void ClearGogglesView(CSteamID steamid)
    {
        EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), false, "Box_Supplier", false);
        EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), false, "Box_Consumer", false);
        EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), false, "Box_Switch", false);
    }
    private void TracePath(UnturnedPlayer player, Vector3 point1, Vector3 point2, EffectAsset pathEffect)
    {
        float distance = Vector3.Distance(point1, point2);

        Vector3 direction = (point2 - point1).normalized;

        TriggerEffectParameters effect = new()
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
        TriggerEffectParameters effect = new()
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
