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
    public class NodeConnectionsService
    {
        private readonly List<NodeConnection> _connections;
        private readonly Resources _resources;

        public delegate void NodeConnectionEventHandler(UnturnedPlayer player, NodeConnection connection);
        public static event NodeConnectionEventHandler OnNodeConnected;
        public static event NodeConnectionEventHandler OnNodeDisconnected;

        public NodeConnectionsService(Resources resources)
        {
            _connections = new List<NodeConnection>();
            _resources = resources;

            WiringToolService.OnNodeLinkRequested += OnNodeLinkRequested;
        }

        public List<NodeConnection> GetAllConnections()
        {
            return _connections;
        }

        private void OnNodeLinkRequested(UnturnedPlayer player, IElectricNode node1, IElectricNode node2)
        {
            var existingConnection = GetConnection(node1, node2);

            if (existingConnection != null)
            {
                DisconnectNodes(player, existingConnection);
            }
            else
            {
                ConnectNodes(player, node1, node2);
            }
        }

        private void ConnectNodes(UnturnedPlayer player, IElectricNode node1, IElectricNode node2)
        {
            NodeConnection connection = new NodeConnection(new List<Vector3>(), node1, node2);

            _connections.Add(connection);

            OnNodeConnected?.Invoke(player, connection);

            RecalculatePower();
        }

        private void DisconnectNodes(UnturnedPlayer player, NodeConnection connection)
        {
            if (_connections.Contains(connection))
            {
                _connections.Remove(connection);

                OnNodeDisconnected?.Invoke(player, connection);

                RecalculatePower();
            }
        }

        public NodeConnection GetConnection(IElectricNode node1, IElectricNode node2)
        {
            return _connections.FirstOrDefault(nc =>
                (nc.Node1 == node1 && nc.Node2 == node2) ||
                (nc.Node1 == node2 && nc.Node2 == node1));
        }

        public void RecalculatePower()
        {
            var graph = BuildGraph();
            var allNodes = graph.Keys.ToList();

            var desiredStates = new Dictionary<IElectricNode, bool>();
            foreach (var node in allNodes)
            {
                desiredStates[node] = false;
            }

            var visited = new HashSet<IElectricNode>();

            foreach (var node in allNodes)
            {
                if (visited.Contains(node)) continue;

                ProcessIsland(node, graph, visited, desiredStates);
            }

            foreach (var kvp in desiredStates)
            {
                IElectricNode node = kvp.Key;
                bool shouldBePowered = kvp.Value;

                if (node.IsPowered != shouldBePowered)
                {
                    node.SetPowered(shouldBePowered);
                }
            }
        }

        private void ProcessIsland(
            IElectricNode startNode,
            Dictionary<IElectricNode, List<IElectricNode>> graph,
            HashSet<IElectricNode> visited,
            Dictionary<IElectricNode, bool> desiredStates)
        {
            var islandNodes = new List<IElectricNode>();
            var suppliers = new List<SupplierNode>();
            var consumers = new List<ConsumerNode>();

            var queue = new Queue<IElectricNode>();
            queue.Enqueue(startNode);
            visited.Add(startNode);
            islandNodes.Add(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current is SupplierNode sup) suppliers.Add(sup);
                if (current is ConsumerNode con) consumers.Add(con);

                if (!current.AllowPowerThrough)
                    continue;

                if (graph.TryGetValue(current, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            islandNodes.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            float totalSupply = suppliers.Sum(s => s.Supply);
            float totalConsumption = consumers.Sum(c => c.Consumption);

            bool hasPower = (totalSupply >= totalConsumption) && (totalSupply > 0);

            if (hasPower)
            {
                foreach (var node in islandNodes)
                {
                    desiredStates[node] = true;
                }
            }
        }

        private Dictionary<IElectricNode, List<IElectricNode>> BuildGraph()
        {
            var adj = new Dictionary<IElectricNode, List<IElectricNode>>();

            foreach (var conn in _connections)
            {
                if (!adj.ContainsKey(conn.Node1)) adj[conn.Node1] = new List<IElectricNode>();
                if (!adj.ContainsKey(conn.Node2)) adj[conn.Node2] = new List<IElectricNode>();

                adj[conn.Node1].Add(conn.Node2);
                adj[conn.Node2].Add(conn.Node1);
            }
            return adj;
        }
    }
}
