using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Nodes;

namespace Wired
{
    public class RadioManager : MonoBehaviour
    {
        public Dictionary<uint, IElectricNode> Nodes;
        private void Awake()
        {
            DebugLogger.Log("Initialized Radiomanager");
        }
        public void Transmit(string frequency, RadioSignalType signal)
        {
            if (!Plugin.Instance.LevelLoaded)
                return;
            List<RadioReceiverNode> receivers = Nodes.Values.OfType<RadioReceiverNode>().Where(r => r.Frequency == frequency).ToList();

            ushort touchedreceivers = 0;
            foreach (RadioReceiverNode receiver in receivers)
            {
                if (receiver.IsOn && signal == RadioSignalType.False || !receiver.IsOn && signal == RadioSignalType.True || signal == RadioSignalType.Toggle)
                {
                    receiver.SetState(signal);
                    touchedreceivers++;
                }
            }
            if(touchedreceivers > 0)
                Plugin.Instance.UpdateAllNetworks();


            DebugLogger.Log($"Transmitted signal {signal} on frequency {frequency}, affected {touchedreceivers} receivers.");
        }
    }
    public enum RadioSignalType
    {
        False,
        True,
        Toggle
    }
}
