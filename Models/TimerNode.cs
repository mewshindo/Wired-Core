using SDG.Unturned;
using System;
using System.Collections;
using UnityEngine;
using Wired.Utilities;

namespace Wired.Models
{
    public class TimerNode : MonoBehaviour, IElectricNode
    {
        private float _remainingTime = 0f;
        public uint InstanceID { get; private set; }
        public bool IsPowered { get; set; }
        public float Consumption { get; set; }
        public ushort DelaySeconds { get; set; }
        public bool AllowPowerThrough { get; private set; }
        
        private bool _isCountingDown;

        private InteractableSign _display;
        private Coroutine _coroutine;

        public void SetPowered(bool powered) { }
        public void Switch(bool state)
        {
            AllowPowerThrough = state;
            var spot = GetComponent<InteractableSpot>();
            BarricadeManager.ServerSetSpotPowered(spot, state);
        }
        private void Awake()
        {
            _display = GetComponent<InteractableSign>();
            if(_display == null)
            {
                WiredLogger.Error($"Some timer is not a sign (what the hell????) \n" +
                    $"Name: {BarricadeManager.FindBarricadeByRootTransform(transform).asset.FriendlyNameWithFriendlyType} ({BarricadeManager.FindBarricadeByRootTransform(transform).asset.GUID})\n" +
                    $"Fix immediately!!! >:(");
            }
            InstanceID = BarricadeManager.FindBarricadeByRootTransform(this.transform).instanceID;
        }
        public void StartTimer()
        {
            if (AllowPowerThrough || _isCountingDown)
                return;

            _remainingTime = DelaySeconds;
            _isCountingDown = true;
            AllowPowerThrough = false;

            WiredLogger.Log($"TimerNode {InstanceID} starting countdown");
            _remainingTime = DelaySeconds;
            _coroutine = StartCoroutine(TimerCoroutine());
        }
        public void StopIfRunning()
        {
            if(_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
            AllowPowerThrough = false;
            _isCountingDown = false;
            _remainingTime = DelaySeconds;

            BarricadeManager.ServerSetSignText(_display, $"{FormattedTime()}");
        }
        private IEnumerator TimerCoroutine()
        {
            while(_remainingTime > 0f)
            {
                yield return new WaitForSeconds(1f);

                _remainingTime--;

                if (_display != null)
                {
                    
                    BarricadeManager.ServerSetSignText(_display, $"{FormattedTime()}");
                }
            }
            _isCountingDown = false;
            AllowPowerThrough = true;
            _coroutine = null;
            Plugin.Instance.SendTimerExpired(this);

            WiredLogger.Log($"TimerNode {InstanceID} Countdown finished");
        }

        private string FormattedTime()
        {
            int totalSeconds = (int)_remainingTime;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            int msTenths = (int)((_remainingTime - totalSeconds) * 10f);
            if (msTenths < 0) msTenths = 0;

            string formattedTime = $"{minutes:D2}:{seconds:D2}";

            return formattedTime;
        }
    }
}