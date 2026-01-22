using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Wired.Models
{
    public class SupplierNode : MonoBehaviour, IElectricNode
    {
        public bool IsPowered { get; private set; }
        public float Consumption { get; }
        public bool AllowPowerThrough { get; } = true;
        public float Supply { get; set; }
        public void SetPowered(bool powered)
        {
            IsPowered = powered;
        }
    }
}
