using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Models;
using Wired.Utilities;

namespace Wired.WiredInteractables
{
    public class RemoteReceiver : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }
        private GateNode _switchNode;

        public bool IsOn => false;

        public string Frequency { get; private set; }

        public void SetPowered(bool state) { }

        private void Awake()
        {
            if (!TryGetComponent(out InteractableSign sign))
            {
                WiredLogger.Error($"Some remote receiver is not a sign (what the hell????) \n" +
                    $"Name: {BarricadeManager.FindBarricadeByRootTransform(transform).asset.FriendlyNameWithFriendlyType} ({BarricadeManager.FindBarricadeByRootTransform(transform).asset.GUID})\n" +
                    $"Fix immediately!!! >:(");

                Destroy(this);
            }
            interactable = sign;

            if (TrySetFrequency(((InteractableSign)interactable).text, null))
            {
                Frequency = ((InteractableSign)interactable).text.Split(' ')[1];
            }
            else
            {
                Frequency = (Mathf.Round((3f + UnityEngine.Random.Range(0.2f, 0.8f)) * 1000f) / 1000f).ToString();
            }

            BarricadeManager.ServerSetSignText((InteractableSign)interactable, $"FREQ {Frequency}");

            _switchNode = GetComponent<GateNode>();

            RemoteTransmitter.OnRemoteTransmissionCommenced += OnRemoteTransmissionCommenced;
        }

        private void OnRemoteTransmissionCommenced(string frequency, RemoteSignalType signal)
        {
            WiredLogger.Info($"Received remote signal on frequency {frequency} with signal {signal}");
            if (frequency == Frequency)
            {
                if(signal != RemoteSignalType.TOGGLE)
                    _switchNode.Switch(signal == RemoteSignalType.ON);
                else
                    _switchNode.Switch(!_switchNode.IsPowered);
            }
        }

        public bool TrySetFrequency(string signinput, Player instigator)
        {
            try
            {
                if (!signinput.StartsWith("FREQ "))
                    throw new Exception();

                var freq = signinput.Split(' ')[1];

                if (freq.Length != 5)
                    throw new Exception();

                if (!float.TryParse(freq, out var f))
                    throw new Exception();

                if (f < 3f || f > 4f)
                    throw new Exception();

                Frequency = freq;
                if (interactable != null)
                {
                    BarricadeManager.ServerSetSignText((InteractableSign)interactable, $"FREQ {Frequency}");
                }
                WiredLogger.Info($"Set frequency to {Frequency}");
            }
            catch (Exception)
            {
                if (instigator != null)
                    instigator.ServerShowHint("Invalid frequency! Please use format: FREQ X.XXX (between 3.000 and 4.000)", 5f);
                return false;
            }
            return true;
        }
    }
}
