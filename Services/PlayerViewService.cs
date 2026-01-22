using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System;
using UnityEngine;
using Wired.Models;

namespace Wired.Services
{
    public class PlayerViewService
    {
        private Resources _resources;
        private readonly NodeConnectionsService _nodeService;
        public PlayerViewService(Resources resources, NodeConnectionsService ns)
        {
            WiringToolService.OnNodeSelected += OnNodeSelected;
            WiringToolService.OnNodeSelectionClearRequested += OnNodeSelectionCleared;
            PlayerEvents.OnDequipRequested += OnDequipRequested;
            _resources = resources;
            _nodeService = ns;
        }

        private void OnDequipRequested(Player player, ItemAsset asset, ref bool shouldAllow)
        {
            player.ServerShowHint("", 0);
        }

        private void OnNodeSelectionCleared(UnturnedPlayer player)
        {
            player.Player.ServerShowHint("Selection cleared.", 2f);
        }

        private void OnNodeSelected(UnturnedPlayer player, UnityEngine.Transform nodeTransform)
        {
            player.Player.ServerShowHint("Click on another thing to link them together!<br>Click anywhere else to cancel.", 5f);
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

                if (!t.TryGetComponent<IElectricNode>(out IElectricNode node))
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
