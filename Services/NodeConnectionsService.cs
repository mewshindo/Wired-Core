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

        public NodeConnectionsService() 
        {
            _nodeToNetwork = new Dictionary<IElectricNode, ElectricNetwork>();
            Networks = new HashSet<ElectricNetwork>();

            WiringToolService.OnNodeLinkRequested += OnNodeLinkRequested;
            Plugin.OnSwitchToggled += OnSwitchToggled;
            Plugin.OnTimerExpired += OnTimerExpired;
            Plugin.OnGeneratorFuelUpdated += OnGeneratorFuelUpdated;
            Plugin.OnGeneratorPoweredChanged += OnGeneratorPoweredChanged;
            PlayerDetector.OnPlayerDetected += PlayerDetector_OnPlayerDetected;
            PlayerDetector.OnPlayerUnDetected += PlayerDetector_OnPlayerUnDetected;
        }

        private void OnGeneratorPoweredChanged(InteractableGenerator generator, bool isPowered)
        {
            if (!generator.TryGetComponent(out SupplierNode gen))
                return;
            if (generator.fuel <= 0)
                return;

            if (_nodeToNetwork.TryGetValue(gen, out ElectricNetwork net))
            {
                if (isPowered && !gen.IsPowered)
                {
                    gen.SetPowered(true);
                    net.RecalculateFlow();
                }
                else if (!isPowered && gen.IsPowered)
                {
                    gen.SetPowered(false);
                    net.RecalculateFlow();
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
                    net.RecalculateFlow();
                }
                else if (newAmount == 0 && gen.IsPowered)
                {
                    gen.SetPowered(false);
                    net.RecalculateFlow();
                }
            }
        }

        private void PlayerDetector_OnPlayerUnDetected(PlayerDetector detector)
        {
            var switchnode = detector.GetComponentInParent<SwitchNode>();
            if (switchnode == null)
            {
                
            }
        }

        private void PlayerDetector_OnPlayerDetected(PlayerDetector detector)
        {
            throw new NotImplementedException();
        }

        private void OnTimerExpired(TimerNode timer)
        {
            if(_nodeToNetwork.TryGetValue(timer, out ElectricNetwork net))
            {
                net.RecalculateFlow();
            }
        }

        private void OnSwitchToggled(SwitchNode sw, bool state)
        {
            if (_nodeToNetwork.TryGetValue(sw, out ElectricNetwork net))
            {
                net.RecalculateFlow();
            }
        }

        public List<NodeConnection> GetAllConnections()
        {
            return Networks.SelectMany(n => n.Connections).ToList();
        }
        public Dictionary<uint, IElectricNode> GetAllNodes()
        {
            return _nodeToNetwork.ToDictionary(kvp => kvp.Key.InstanceID, kvp => kvp.Key);
        }

        private void OnNodeLinkRequested(UnturnedPlayer player, IElectricNode node1, IElectricNode node2, List<Vector3> wirepath)
        {
            var existingConnection = GetConnection(node1, node2);

            if (existingConnection != null)
            {
                DisconnectNodes(player, existingConnection);
            }
            else
            {
                ConnectNodes(player, node1, node2, wirepath);
            }
        }

        public void LoadConnection(IElectricNode node1, IElectricNode node2, List<Vector3> wirePath)
        {
            ConnectNodes(null, node1, node2, wirePath);
        }
        private void ConnectNodes(UnturnedPlayer player, IElectricNode node1, IElectricNode node2, List<Vector3> wirePath)
        {
            NodeConnection connection = new NodeConnection(wirePath ?? new List<Vector3>(), node1, node2);

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
                net1.RecalculateFlow();
            }
            else
            {
                MergeNetworks(net1, net2, connection);
            }
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
            ElectricNetwork net = new ElectricNetwork();
            Networks.Add(net);

            RegisterNode(net, initialConnection.Node1);
            RegisterNode(net, initialConnection.Node2);
            net.AddConnection(initialConnection);

            net.RecalculateFlow();
        }

        private void AddToNetwork(ElectricNetwork net, IElectricNode newNode, NodeConnection connection)
        {
            RegisterNode(net, newNode);
            net.AddConnection(connection);
            net.RecalculateFlow();
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

            to.RecalculateFlow();
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

            HashSet<IElectricNode> visited = new HashSet<IElectricNode>();

            foreach (var node in allNodes)
            {
                if (visited.Contains(node)) continue;

                bool isOrphan = !allConnections.Any(c => c.Node1 == node || c.Node2 == node);

                if (isOrphan)
                {
                    if (node.IsPowered) node.SetPowered(false);
                    continue;
                }

                ElectricNetwork newNet = new ElectricNetwork();
                Networks.Add(newNet);

                Queue<IElectricNode> q = new Queue<IElectricNode>();
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

                newNet.RecalculateFlow();
            }
        }

        private void RegisterNode(ElectricNetwork net, IElectricNode node)
        {
            net.AddNode(node);
            _nodeToNetwork[node] = net;
        }
    }
}
