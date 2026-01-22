using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Wired.Models
{
    public class NodeConnection
    {
        public IElectricNode Node1 { get; set; }
        public IElectricNode Node2 { get; set; }
        public List<Vector3> WirePath { get; }
        public NodeConnection(List<Vector3> wirePath, IElectricNode node1, IElectricNode node2)
        {
            WirePath = wirePath ?? new List<Vector3>();
            Node1 = node1;
            Node2 = node2;
        }
    }
}
