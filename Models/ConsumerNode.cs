using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;
using Wired.Wrappers;

namespace Wired.Models
{
    public class ConsumerNode : MonoBehaviour, IElectricNode
    {
        public bool IsPowered { get; set; }
        public float Consumption { get; set; }
        public bool AllowPowerThrough { get; set; } = true;

        private ConsumerInteractable Interactable;
        public void SetPowered(bool powered)
        {
            if(powered == IsPowered)
                return;

            IsPowered = powered;
            Interactable.SetPowered(powered);
        }
    }
}
