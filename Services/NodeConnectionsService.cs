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
using Wired.WiredInteractables;

namespace Wired.Services
{
    public class NodeConnectionsService
    {
        private readonly Dictionary<IElectricNode, ElectricNetwork> _nodeToNetwork;
        public HashSet<ElectricNetwork> Networks { get; private set; }

        public delegate void NodeConnectionEventHandler(UnturnedPlayer player, NodeConnection connection);
        public static event NodeConnectionEventHandler OnNodeConnected;
        public static event NodeConnectionEventHandler OnNodeDisconnected;

        private static NodeConnectionsService Instance;
        public NodeConnectionsService() 
        {
            Instance = this;

            _nodeToNetwork = [];
            Networks = [];

            WiringToolService.OnNodeLinkRequested += OnNodeLinkRequested;
            Plugin.OnGateToggled += OnSwitchToggled;
            Plugin.OnTimerExpired += OnTimerExpired;
            Plugin.OnGeneratorFuelUpdated += OnGeneratorFuelUpdated;
            Plugin.OnGeneratorPoweredChanged += OnGeneratorPoweredChanged;
            Plugin.OnBarricadeDestroyed += OnNodeDestroyed;
        }

        public void OnNodeDestroyed(BarricadeDrop drop)
        {
            if(drop.model.TryGetComponent(out IElectricNode node))
            if(node is GateNode gn)
            {
                if(gn.TryGetComponent(out LogicGate lg))
                {
                    foreach (var n in GetAllConnections().Where(con => (object)con.Node1 == lg.Input0 || (object)con.Node2 == lg.Input0))
                    {
                        DisconnectNodes(null, n);
                    }
                    foreach (var n in GetAllConnections().Where(con => (object)con.Node1 == lg.Input1 || (object)con.Node2 == lg.Input1))
                    {
                        DisconnectNodes(null, n);
                    }
                }
            }
            foreach (var n in GetAllConnections().Where(con => con.Node1 == node || con.Node2 == node))
            {
                DisconnectNodes(null, n);

                node.Uninitialize();
            }
            if(drop.model.TryGetComponent(out IWiredInteractable wa))
            {
                wa.Uninitialize();
            }
        }

        public static void RecalculatePowerForNode(IElectricNode node)
        {
            if (Instance._nodeToNetwork.TryGetValue(node, out ElectricNetwork net))
            {
                net.RecalculatePower();
            }
        }

        private void OnGeneratorPoweredChanged(InteractableGenerator generator, bool isPowered)
        {
            if (!generator.TryGetComponent(out SupplierNode gen))
                return;
            if (generator.TryGetComponent<Battery>(out _))
                return;
            if (generator.fuel <= 0)
                return;

            if (_nodeToNetwork.TryGetValue(gen, out ElectricNetwork net))
            {
                if (isPowered && !gen.IsPowered)
                {
                    gen.SetPowered(true);
                    net.RecalculatePower();
                }
                else if (!isPowered && gen.IsPowered)
                {
                    gen.SetPowered(false);
                    net.RecalculatePower();
                }
            }
        }

        private void OnGeneratorFuelUpdated(InteractableGenerator generator, ushort newAmount)
        {
            if (!generator.TryGetComponent(out SupplierNode gen))
                return;

            if (_nodeToNetwork.TryGetValue(gen, out ElectricNetwork net))
            {
                if (newAmount > 0 && !gen.IsPowered)
                {
                    gen.SetPowered(true);
                    net.RecalculatePower();
                }
                else if (newAmount == 0 && gen.IsPowered)
                {
                    gen.SetPowered(false);
                    net.RecalculatePower();
                }
            }
        }

        private void OnTimerExpired(TimerNode timer)
        {
            if(_nodeToNetwork.TryGetValue(timer, out ElectricNetwork net))
            {
                net.RecalculatePower();
            }
        }

        private void OnSwitchToggled(GateNode sw, bool state)
        {
            if (_nodeToNetwork.TryGetValue(sw, out ElectricNetwork net))
            {
                net.RecalculatePower();
            }
        }

        public List<NodeConnection> GetAllConnections()
        {
            return Networks.SelectMany(n => n.Connections).ToList();
        }
        public List<KeyValuePair<uint, IElectricNode>> GetAllNodes()
        {
            List<KeyValuePair<uint, IElectricNode>> result = [];
            foreach(var key in _nodeToNetwork.Keys)
            {
                result.Add(new KeyValuePair<uint, IElectricNode>(key.InstanceID, key));
            }
            return result;
        }
        public bool IsConnected(IElectricNode node1, IElectricNode node2) 
            => GetAllConnections().Any(nc => (nc.Node1 == node1 && nc.Node2 == node2) || (nc.Node1 == node2 && nc.Node2 == node1));

        private void OnNodeLinkRequested(UnturnedPlayer player, IElectricNode node1, IElectricNode node2, List<Vector3> wirepath)
        {
            var existingConnection = GetConnection(node1, node2);

            if (existingConnection != null)
            {
                DisconnectNodes(player, existingConnection);
                NPCEventManager.BroadcastEvent(player.Player, "WiringTool:Message_Unlinked", ENPCEventReplicationMode.InstigatorOnly);
            }
            else
            {
                ConnectNodes(player, node1, node2);
                NPCEventManager.BroadcastEvent(player.Player, "WiringTool:Message_Linked", ENPCEventReplicationMode.InstigatorOnly);
            }
        }

        public void LoadConnection(IElectricNode node1, IElectricNode node2)
        {
            ConnectNodes(null, node1, node2);
        }
        private void ConnectNodes(UnturnedPlayer player, IElectricNode node1, IElectricNode node2)
        {
            NodeConnection connection = new(node1, node2);

            _nodeToNetwork.TryGetValue(node1, out ElectricNetwork net1);
            _nodeToNetwork.TryGetValue(node2, out ElectricNetwork net2);

            if (net1 == null && net2 == null)
            {
                CreateNewNetwork(connection);
            }
            else if (net1 != null && net2 == null)
            {
                AddToNetwork(net1, node2, connection);
            }
            else if (net1 == null && net2 != null)
            {
                AddToNetwork(net2, node1, connection);
            }
            else if (net1 == net2)
            {
                net1.AddConnection(connection);
                net1.RecalculatePower();
            }
            else
            {
                MergeNetworks(net1, net2, connection);
            }
            if(node1 is LogicGateSubnode lgs)
                lgs.IsBusy = true;
            if(node2 is LogicGateSubnode lgs2)
                lgs2.IsBusy = true;

            if(player != null)
            {
                OnNodeConnected?.Invoke(player, connection);
            }
        }

        private void DisconnectNodes(UnturnedPlayer player, NodeConnection connection)
        {
            if (_nodeToNetwork.TryGetValue(connection.Node1, out ElectricNetwork network))
            {
                if (network.Connections.Contains(connection))
                {
                    network.Connections.Remove(connection);

                    RebuildNetworkTopology(network);

                    if(connection.Node1 is LogicGateSubnode lgs)
                        lgs.IsBusy = false;
                    if(connection.Node2 is LogicGateSubnode lgs2)
                        lgs2.IsBusy = false;

                    OnNodeDisconnected?.Invoke(player, connection);
                }
            }
        }

        public NodeConnection GetConnection(IElectricNode node1, IElectricNode node2)
        {
            if (_nodeToNetwork.TryGetValue(node1, out ElectricNetwork net1))
            {
                return net1.Connections.FirstOrDefault(nc =>
                    (nc.Node1 == node1 && nc.Node2 == node2) ||
                    (nc.Node1 == node2 && nc.Node2 == node1));
            }
            return null; // they must be in the same network if they're connected
        }

        private void CreateNewNetwork(NodeConnection initialConnection)
        {
            ElectricNetwork net = new();
            Networks.Add(net);

            RegisterNode(net, initialConnection.Node1);
            RegisterNode(net, initialConnection.Node2);
            net.AddConnection(initialConnection);

            net.RecalculatePower();
        }

        private void AddToNetwork(ElectricNetwork net, IElectricNode newNode, NodeConnection connection)
        {
            RegisterNode(net, newNode);
            net.AddConnection(connection);
            net.RecalculatePower();
        }

        private void MergeNetworks(ElectricNetwork to, ElectricNetwork from, NodeConnection con)
        {
            foreach (var node in from.Nodes)
            {
                RegisterNode(to, node);
            }

            foreach (var conn in from.Connections)
            {
                to.AddConnection(conn);
            }

            to.AddConnection(con);

            Networks.Remove(from);

            to.RecalculatePower();
        }

        /// <summary>
        /// this gets called when a node gets removed from a network
        /// </summary>
        private void RebuildNetworkTopology(ElectricNetwork oldNetwork)
        {
            var allConnections = new List<NodeConnection>(oldNetwork.Connections);
            var allNodes = new List<IElectricNode>(oldNetwork.Nodes);

            Networks.Remove(oldNetwork);
            foreach (var node in allNodes) _nodeToNetwork.Remove(node);

            HashSet<IElectricNode> visited = [];

            foreach (var node in allNodes)
            {
                if (visited.Contains(node)) continue;

                bool isOrphan = !allConnections.Any(c => c.Node1 == node || c.Node2 == node);

                if (isOrphan && node is not SupplierNode)
                {
                    if (node.IsPowered) node.SetPowered(false);
                    continue;
                }

                ElectricNetwork newNet = new();
                Networks.Add(newNet);

                Queue<IElectricNode> q = new();
                q.Enqueue(node);
                visited.Add(node);
                RegisterNode(newNet, node);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    for (int i = allConnections.Count - 1; i >= 0; i--)
                    {
                        var conn = allConnections[i];
                        IElectricNode neighbor = null;

                        if (conn.Node1 == cur) neighbor = conn.Node2;
                        else if (conn.Node2 == cur) neighbor = conn.Node1;

                        if (neighbor != null)
                        {
                            newNet.AddConnection(conn);
                            allConnections.RemoveAt(i);

                            if (!visited.Contains(neighbor))
                            {
                                visited.Add(neighbor);
                                RegisterNode(newNet, neighbor);
                                q.Enqueue(neighbor);
                            }
                        }
                    }
                }

                newNet.RecalculatePower();
            }
        }

        private void RegisterNode(ElectricNetwork net, IElectricNode node)
        {
            net.AddNode(node);
            _nodeToNetwork[node] = net;
        }
    }
}
