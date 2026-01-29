using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wired.Utilities;

namespace Wired.Models
{
    public class ElectricNetwork
    {
        public HashSet<IElectricNode> Nodes { get; private set; } = new HashSet<IElectricNode>();
        public HashSet<NodeConnection> Connections { get; private set; } = new HashSet<NodeConnection>();

        public float TotalSupply { get; private set; }
        public float TotalConsumption { get; private set; }

        public void AddNode(IElectricNode node) => Nodes.Add(node);
        public void AddConnection(NodeConnection conn) => Connections.Add(conn);

        public void RecalculateFlow()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            HashSet<IElectricNode> visited = new HashSet<IElectricNode>();

            var adjacencyMap = BuildAdjacencyMap();

            foreach (var startNode in Nodes)
            {
                if (visited.Contains(startNode))
                    continue;

                List<IElectricNode> islandNodes = new List<IElectricNode>();
                float currentIslandSupply = 0f;
                float currentIslandConsumption = 0f;

                Queue<IElectricNode> queue = new Queue<IElectricNode>();
                queue.Enqueue(startNode);
                visited.Add(startNode);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    islandNodes.Add(current);

                    if (current is SupplierNode sup) currentIslandSupply += sup.Supply;
                    if (current is ConsumerNode cons) currentIslandConsumption += cons.Consumption;

                    if (!current.AllowPowerThrough)
                        continue;

                    if (adjacencyMap.ContainsKey(current))
                    {
                        foreach (var neighbor in adjacencyMap[current])
                        {
                            if (!visited.Contains(neighbor))
                            {
                                visited.Add(neighbor);
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }

                bool hasEnoughPower = currentIslandSupply > 0 &&
                                      currentIslandSupply >= currentIslandConsumption;

                foreach (var node in islandNodes)
                {
                    if(node is TimerNode timer)
                    {
                        if (hasEnoughPower)
                            timer.StartTimer();
                        else
                            timer.StopIfRunning();
                        continue;
                    }
                    if (node.IsPowered != hasEnoughPower)
                    {
                        node.SetPowered(hasEnoughPower);
                    }
                }
            }

            sw.Stop();
            WiredLogger.Log($"Recalculated power flow in a {Nodes.Count}-node network, {sw.ElapsedMilliseconds} ms.");
        }

        private Dictionary<IElectricNode, List<IElectricNode>> BuildAdjacencyMap()
        {
            var map = new Dictionary<IElectricNode, List<IElectricNode>>();

            foreach (var conn in Connections)
            {
                if (!map.ContainsKey(conn.Node1)) map[conn.Node1] = new List<IElectricNode>();
                if (!map.ContainsKey(conn.Node2)) map[conn.Node2] = new List<IElectricNode>();

                map[conn.Node1].Add(conn.Node2);
                map[conn.Node2].Add(conn.Node1);
            }
            return map;
        }
    }
}
