using System.Collections.Generic;
using UnityEngine;

namespace Wired.Models
{
    public class NodeConnection
    {
        public IElectricNode Node1 { get; set; }
        public IElectricNode Node2 { get; set; }
        public NodeConnection(IElectricNode node1, IElectricNode node2)
        {
            Node1 = node1;
            Node2 = node2;
        }
    }
}
