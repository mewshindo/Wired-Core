using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Utilities;

namespace Wired.WiredInteractables
{
    public class RemoteTransmitter : MonoBehaviour, IWiredInteractable
    {
        public delegate void RemoteTransmissionCommenced(string frequency, RemoteSignalType signal);
        public static event RemoteTransmissionCommenced OnRemoteTransmissionCommenced;

        public Interactable interactable { get; private set; }

        public bool IsOn { get; private set; } = false;

        public string Frequency { get; private set; }

        public float Range { get; set; }

        public void SetPowered(bool state)
        {
            IsOn = state;
            WiredLogger.Info($"Remote transmitter on frequency {Frequency} turned {(IsOn ? "ON" : "OFF")}");
            if (IsOn)
            {
                OnRemoteTransmissionCommenced?.Invoke(Frequency, RemoteSignalType.ON);
            }
            else
            {
                OnRemoteTransmissionCommenced?.Invoke(Frequency, RemoteSignalType.OFF);
            }
        }

        private void Awake()
        {
            if (!TryGetComponent(out InteractableSign sign))
            {
                WiredLogger.Error($"Some remote transmitter is not a sign (what the hell????) \n" +
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
