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
using SDG.NetTransport;
using static SDG.Unturned.WeatherAsset;

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
    private bool _viewUpdateFrameLocked = false;
    private Dictionary<CSteamID, Dictionary<string, string>> _cachedUIText = [];

    private float _lastUpdateTime = 0f;
    private const float UPDATE_RATE = 0.1f;

    private const short PonderUIKey = 26000;
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
        NPCEventManager.onEvent += onEvent;
        EffectManager.onEffectButtonClicked += onEffectButtonClicked;

        U.Events.OnPlayerConnected += OnPlayerConnected;
        
        _assets = assets;
        _resources = resources;
        _ncs = ncs;
        _selectedNode = selectedNode;
    }

    private void onEffectButtonClicked(Player player, string buttonName)
    {
        if (buttonName.EndsWith("_Close"))
        {
            WiredLogger.Info($"Received effect button click, \"{buttonName}\", trying to find {buttonName.Replace("_Close", string.Empty)}");
            if (_resources.PonderEffects.TryGetValue(buttonName.Replace("_Close", string.Empty), out EffectAsset asset))
            {
                ClearPonderScene(player, asset);
            }
        }
    }

    private void onEvent(Player instigatingPlayer, string eventId)
    {
        WiredLogger.Info($"Received NPCGlobalEvent {eventId}");
        if (eventId.EndsWith("_Ponder"))
        {
            if (_resources.PonderEffects.TryGetValue(eventId, out EffectAsset effect))
            {
                if (effect == null) return;
                EffectManager.SendUIEffect(effect, PonderUIKey, instigatingPlayer.channel.owner.transportConnection, true);
                instigatingPlayer.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
            }
        }
    }

    private void OnBarricadeMoveRequested(CSteamID instigator, byte x, byte y, ushort plant, uint instanceID, ref Vector3 point, ref byte angle_x, ref byte angle_y, ref byte angle_z, ref bool shouldAllow)
    {
        if (_viewUpdateFrameLocked) return;
        WiredLogger.Info($"Barricade move requetsed to {point}");
        _viewUpdateFrameLocked = true;
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
        StartCoroutine(SendWires(player));
        var glasses = player.Player.clothing.glassesAsset;
        if (glasses == null) return;
        if (_assets.WiredAssets.ContainsKey(glasses.GUID) && (_assets.WiredAssets[glasses.GUID] is EngineerGogglesAsset))
        {
            _playersWithGogglesOn.Add(player);
        }
    }
    private IEnumerator SendWires(UnturnedPlayer player)
    {
        int i = 0;

        int spawnsThisFrame = 0;
        foreach (var con in _ncs.GetAllConnections())
        {
            i++;
            spawnsThisFrame++;

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
                relevantDistance = 4096f,
                shouldReplicate = true,
                reliable = true,
                scale = new Vector3(1f, 1f, distance * scalemodifier)
            };
            effect.SetDirection(direction);
            effect.SetRelevantPlayer(player.Player);
            EffectManager.triggerEffect(effect);

            if (spawnsThisFrame >= 25)
            {
                spawnsThisFrame = 0;
                yield return null;
            }
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
        NPCEventManager.BroadcastEvent(player.Player, "WiringTool:Clear", ENPCEventReplicationMode.InstigatorOnly);
        _playersInLinkingMode.Remove(player);
        _lookingAt.Remove(player.CSteamID);
        ClearPreviewView(player.CSteamID);
        ClearSelectedView(player.CSteamID);
    }

    private void OnNodeSelected(UnturnedPlayer player, Transform nodeTransform)
    {
        NPCEventManager.BroadcastEvent(player.Player, "WiringTool:Message_Linking", ENPCEventReplicationMode.InstigatorOnly);
        _selectedNode[player.CSteamID] = nodeTransform;
        _playersInLinkingMode.Add(player);
        _lookingAt[player.CSteamID] = 0;
    }



    private void Update()
    {
        if (Time.time - _lastUpdateTime < UPDATE_RATE) return;
        _lastUpdateTime = Time.time;

        _viewUpdateFrameLocked = false;

        foreach (var steamplayer in Provider.clients)
        {
            var player = UnturnedPlayer.FromSteamPlayer(steamplayer);
            var steamid = player.CSteamID;

            bool holdingWiringTool = false;
            if (player.Player.equipment.asset != null)
            {
                if (_assets.WiredAssets.TryGetValue(player.Player.equipment.asset.GUID, out var asset) && asset is WiringToolAsset)
                {
                    holdingWiringTool = true;
                }
            }

            bool hasGoggles = _playersWithGogglesOn.Contains(player);
            if (!hasGoggles && !holdingWiringTool)
                continue;

            Raycast ray = new(player.Player, 16);
            BarricadeDrop drop = ray.GetBarricade(out _, out float distance, out LogicGateSubnode lgs);

            _selectedNode.TryGetValue(steamid, out Transform selectedTransform);

            if (drop == null || !DoesOwnDrop(drop, steamid))
            {
                HandleLookAway(steamid);
                continue;
            }

            uint lookingatID = drop.instanceID;
            if (!drop.model.TryGetComponent(out IElectricNode node))
            {
                HandleLookAway(steamid);
                _lookingAt[steamid] = lookingatID;
                continue;
            }

            _lookingAt.TryGetValue(steamid, out uint previousLookID);
            bool targetChanged = previousLookID != lookingatID;
            if (hasGoggles)
            {
                if (distance > 4)
                {
                    if (_cachedUIText.ContainsKey(steamid) && _cachedUIText[steamid].Count > 0)
                        ClearGogglesView(steamid);
                }
                else
                {
                    if (targetChanged)
                    {
                        ClearGogglesView(steamid);
                        EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, "Container", true);
                    }
                    UpdateGogglesView(steamid, drop);
                }
            }
            if (targetChanged)
            {
                ClearSelectedView(steamid);

                if (lgs == null && holdingWiringTool)
                {
                    SendNodeSelectedEffect(player, drop.model.position, node);
                }
                else if (holdingWiringTool)
                {
                    sendEffectCool(player, lgs.transform.position, _resources.node_subnode_selected);
                }
            }

            _lookingAt[steamid] = lookingatID;

            if (player.Player == null || selectedTransform == null)
            {
                _playersInLinkingMode.Remove(player);
                _lookingAt.Remove(steamid);
                continue;
            }

            if (drop.model == selectedTransform || !targetChanged)
            {
                continue;
            }

            if (_playersInLinkingMode.Contains(player))
            {
                IElectricNode selectedNodeComponent = selectedTransform.GetComponent<IElectricNode>();

                if (_ncs.GetConnection(node, selectedNodeComponent) != null)
                {
                    ClearPreviewView(steamid);
                    continue;
                }

                ClearPreviewView(steamid);
                EffectAsset effect = GetPreviewEffectForNodes(selectedNodeComponent, node);
                SendNodeSelectedEffect(player, drop.model.position, node);
                if(lgs == null)
                    TracePath(player, selectedTransform.position, drop.model.position, effect);
                else
                    TracePath(player, selectedTransform.position, lgs.transform.position, effect);

            }
        }
    }

    private void HandleLookAway(CSteamID steamid)
    {
        if (_lookingAt.TryGetValue(steamid, out uint prevId) && prevId != 0)
        {
            _lookingAt[steamid] = 0;
            ClearPreviewView(steamid);
            ClearSelectedView(steamid);
        }
        if(_cachedUIText.TryGetValue(steamid, out var cache) && cache.Count > 0)
            ClearGogglesView(steamid);
    }

    private void SendNodeSelectedEffect(UnturnedPlayer player, Vector3 pos, IElectricNode node)
    {
        EffectAsset asset = node switch
        {
            GateNode => _resources.node_gate_selected,
            TimerNode => _resources.node_gate_selected,
            LogicGateSubnode => _resources.node_subnode_selected,
            ConsumerNode => _resources.node_consumer_selected,
            SupplierNode => _resources.node_power_selected,
            _ => null
        };
        if (asset != null) sendEffectCool(player, pos, asset);
    }

    private EffectAsset GetPreviewEffectForNodes(IElectricNode node1, IElectricNode node2)
    {
        if (node1 is SupplierNode || node2 is SupplierNode) return _resources.preview_power;
        if (node1 is TimerNode || node2 is TimerNode) return _resources.preview_gate;
        if (node1 is LogicGateSubnode || node2 is LogicGateSubnode) return _resources.preview_subnode;
        if (node1 is GateNode || node2 is GateNode) return _resources.preview_gate;
        return _resources.preview_consumer;
    }

    private void UpdateNodesView(CSteamID steamid) => StartCoroutine(UpdateNodesViewCoroutine(steamid));
    private IEnumerator UpdateNodesViewCoroutine(CSteamID steamid)
    {
        foreach (Guid guid in _resources.nodeeffects)
            EffectManager.ClearEffectByGuid(guid, Provider.findTransportConnection(steamid));

        yield return null;

        UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamid);
        if (player == null) yield break;

        HashSet<IElectricNode> visibleNodes = [];

        var bfinder = new BarricadeFinder(player.Position);
        foreach (BarricadeDrop drop in bfinder.GetBarricadesInRadius(128f))
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
            if (!selectedNode.TryGetComponent(out IElectricNode node)) yield break;

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
        StartCoroutine(UpdateWiresCoroutine());
    }

    private IEnumerator UpdateWiresCoroutine()
    {
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_2m.GUID);
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_4m.GUID);
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_6m.GUID);
        EffectManager.ClearEffectByGuid_AllPlayers(_resources.wire_8m.GUID);

        yield return null;

        int i = 0;
        int spawnsThisFrame = 0;

        foreach (var con in _ncs.GetAllConnections())
        {
            i++;
            spawnsThisFrame++;

            var distance = Vector3.Distance(con.Node1.WireConnectPoint.position, con.Node2.WireConnectPoint.position);
            if (distance <= 1) continue;

            EffectAsset wire;
            float scalemodifier;

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
                relevantDistance = 4096f,
                shouldReplicate = true,
                reliable = true,
                scale = new Vector3(1f, 1f, distance * scalemodifier)
            };
            effect.SetDirection(direction);
            EffectManager.triggerEffect(effect);

            if (spawnsThisFrame >= 25)
            {
                spawnsThisFrame = 0;
                yield return null;
            }
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
                SendGogglesUIText(steamid, "Text_Name", $"<color=#FF9F00>{drop.asset.FriendlyName}");
                SendGogglesUIText(steamid, "Text_0", $"Supply: <color=#00eeff>{Math.Round(sup.Supply, 1)}pu");
                if(sup.TryGetComponent(out Battery battery))
                {
                    SendGogglesUIText(steamid, "Text_1", $"Charge: <color=#00eeff>{Math.Round((battery.Charge/battery.MaxCapacity) * 100)}%");
                    SendGogglesUIText(steamid, "Text_2", node.IsPowered ? "Powered: <color=#00eeff>Yes" : "Powered: <color=#00eeff>No");
                }
                else if(sup.TryGetComponent(out SolarPanel solarpanel))
                {
                    SendGogglesUIText(steamid, "Text_1", $"Efficiency: <color=#00eeff>{Math.Round(solarpanel.Efficiency * 100)}%" + 
                        (solarpanel.IsSunBlocked ? " (No direct sunlight)" : string.Empty));
                    SendGogglesUIText(steamid, "Text_2", node.IsPowered ? "Powered: <color=#00eeff>Yes" : "Powered: <color=#00eeff>No");
                }
                else
                {
                    SendGogglesUIText(steamid, "Text_1", node.IsPowered ? "Powered: <color=#00eeff>Yes" : "Powered: <color=#00eeff>No");
                }
                break;
            case ConsumerNode cons:
                SendGogglesUIText(steamid, "Text_Name", $"<color=#6AFF2A>{drop.asset.FriendlyName}");
                SendGogglesUIText(steamid, "Text_0", $"Consumption: <color=#00eeff>{Math.Round(cons.Consumption, 1)}pu");
                if (cons.TryGetComponent(out RemoteTransmitter transmitter))
                {
                    SendGogglesUIText(steamid, "Text_1", $"Frequency: <color=#00eeff>{transmitter.Frequency}");
                    SendGogglesUIText(steamid, "Text_2", node.IsPowered ? "Powered: <color=#00eeff>Yes" : "Powered: <color=#00eeff>No");
                }
                else
                {
                    SendGogglesUIText(steamid, "Text_1", node.IsPowered ? "Powered: <color=#00eeff>Yes" : "Powered: <color=#00eeff>No");
                }
                break;
            case GateNode sw:
                SendGogglesUIText(steamid, "Text_Name", $"<color=#2ACAFF>{drop.asset.FriendlyName}");
                SendGogglesUIText(steamid, "Text_0", $"Gate: " + (sw.AllowPowerThrough ? "<color=#00eeff>Open" : "<color=#00eeff>Closed"));
                if(sw.TryGetComponent(out RemoteReceiver receiver))
                {
                    SendGogglesUIText(steamid, "Text_1", $"Frequency: <color=#00eeff>{receiver.Frequency}");
                    break;
                }
                if(sw.TryGetComponent(out LogicGate lg))
                {
                    SendGogglesUIText(steamid, "Text_1", $"Type: <color=#00eeff>{lg.Type}");
                    SendGogglesUIText(steamid, "Text_2", $"Input 1: " + (lg.Input0.IsPowered ? "<color=#00eeff>True" : "<color=#00eeff>False"));
                    SendGogglesUIText(steamid, "Text_3", $"Input 2: " + (lg.Input1.IsPowered ? "<color=#00eeff>True" : "<color=#00eeff>False"));
                    break;
                }
                if(sw.TryGetComponent(out Keypad kp))
                {
                    if(drop.GetServersideData().owner == steamid.m_SteamID)
                    {
                        SendGogglesUIText(steamid, "Text_1", $"Code: " + (kp.Code == string.Empty ? "<color=#00eeff>Not set" : $"<color=#00eeff>{kp.Code}"));
                    }
                    break;
                }
                if(sw.TryGetComponent(out Button button))
                {
                    SendGogglesUIText(steamid, "Text_1", $"Action time: <color=#00eeff>{button.StaysPressedSeconds} seconds");
                    break;
                }
                if(sw.TryGetComponent(out DaylightSensor dns))
                {
                    var asset = sw.Asset as DaylightSensorAsset;
                    SendGogglesUIText(steamid, "Text_1", $"Mode: {asset.Mode}");
                    if(dns.IsObstructed)
                        SendGogglesUIText(steamid, "Text_2", $"Sunlight obstructed");
                }
                break;
            case TimerNode tn:
                SendGogglesUIText(steamid, "Text_Name", $"<color=#2ACAFF>{drop.asset.FriendlyName}");
                SendGogglesUIText(steamid, "Text_0", $"Gate: " + (tn.AllowPowerThrough ? "<color=#00eeff>Open" : "<color=#00eeff>Closed"));
                SendGogglesUIText(steamid, "Text_1", $"Delay: <color=#00eeff>{tn.DelaySeconds} seconds");

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
        EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), false, "Container", false);

        if (_cachedUIText != null && _cachedUIText.ContainsKey(steamid))
        {
            _cachedUIText[steamid].Clear();
        }
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
    private void SendGogglesUIText(CSteamID steamid, string gameobjorpath, string text)
    {
        if (!_cachedUIText.ContainsKey(steamid))
        {
            _cachedUIText[steamid] = new Dictionary<string, string>();
        }

        if (_cachedUIText[steamid].TryGetValue(gameobjorpath, out string lastText) && lastText == text)
        {
            return;
        }

        _cachedUIText[steamid][gameobjorpath] = text;

        if (gameobjorpath.StartsWith("Text_"))
        {
            var field = "Field_" + gameobjorpath.Split('_')[1];
            EffectManager.sendUIEffectVisibility(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, field, true);
        }
        EffectManager.sendUIEffectText(Resources.GogglesUIKey, Provider.findTransportConnection(steamid), true, gameobjorpath, text);
    }

    private void SendPonderScene(Player player, EffectAsset scene)
    {
        if (scene == null) return;
        EffectManager.SendUIEffect(scene, PonderUIKey, player.channel.owner.transportConnection, true);
        player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
    }
    private void ClearPonderScene(Player player, EffectAsset asset)
    {
        EffectManager.ClearEffectByGuid(asset.GUID, player.channel.owner.transportConnection);
        player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
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
}
