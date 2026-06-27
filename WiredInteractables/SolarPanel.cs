using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using UnityEngine;
using Wired.Models;
using Wired.Services;
using Wired.Utilities;
using Wired.WiredAssets;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace Wired.WiredInteractables
{
    internal class SolarPanel : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }
        public SolarPanelAsset Asset { get; set; }
        private SupplierNode _supplierNode;

        public bool IsOn { get; }

        private float _azimuth;
        private float _bias;

        public Vector3 PanelNormal;
        public Transform MovingPart;
        private Transform _MovingPartGameobj;

        private bool _movesToDefaultPosition;

        public void SetPowered(bool state)
        {

        }

        private void Awake()
        {
            if (!TryGetComponent(out InteractableSpot spot))
            {
                Destroy(this);
                return;
            }
            interactable = spot;

            Plugin.OnTimeOfDayUpdated += OnTimeOfDayUpdated;
            _supplierNode = gameObject.GetComponent<SupplierNode>();

            _MovingPartGameobj = transform.Find("MovingPart");
        }
        private void Update()
        {
            if (!_movesToDefaultPosition) return;
            if(Math.Abs(_MovingPartGameobj.localEulerAngles.x) < 1f)
            {
                _movesToDefaultPosition = false;
                return;
            }

            var angleDelta = _MovingPartGameobj.localEulerAngles.x > 180 ? 1f : -1f;

            _MovingPartGameobj.Rotate(new Vector3(angleDelta, 0, 0), Space.Self);
            BarricadeManager.ServerSetBarricadeTransform(MovingPart, _MovingPartGameobj.position, _MovingPartGameobj.rotation);
        }

        private void OnTimeOfDayUpdated(uint timeOfDay, float timefraction)
        {
            RotateMovingPart(out float sunangle);
            
            var efficiency = GetSolarPanelEfficiency(sunangle);

            var newsupply = Asset.Supply * efficiency;
            _supplierNode.Supply = newsupply;
            NodeConnectionsService.RecalculatePowerForNode(_supplierNode);

            if (MovingPart == null) return;

            WiredLogger.Info($"Current supply: {_supplierNode.Supply}");
        }

        private float GetSolarPanelEfficiency(float sunangle)
        {
            Quaternion sunRotation = Quaternion.Euler(-sunangle, LevelLighting.azimuth, 0f);
            Vector3 sunDirection = sunRotation * Vector3.forward;

            if(MovingPart != null)
            {
                PanelNormal = new Vector3(-MovingPart.forward.x, MovingPart.forward.y, -MovingPart.forward.z);
            }

            float dot = Vector3.Dot(PanelNormal, sunDirection.normalized);

            WiredLogger.Info($"Efficiency: {Mathf.Max(0f, dot)}, PanelNormal direction: {PanelNormal}, sunDirection: {sunDirection}");

            return Mathf.Max(0f, dot);
        }
        private void RotateMovingPart(out float sunangle)
        {
            var bias = LevelLighting.bias;
            var truetime = (float)LightingManager.time / (float)LightingManager.cycle;
            sunangle = Math.Abs((truetime / bias * 180f) / 1f - bias);

            if (MovingPart == null) return;

            if(sunangle - 90f > Asset.MovingPartMaxAngle)
            {
                _movesToDefaultPosition = true;
                return;
            }
            _movesToDefaultPosition = false;

            Quaternion sunRotation = Quaternion.Euler(-sunangle, LevelLighting.azimuth, 0f);

            Vector3 sunWorldDirection = sunRotation * Vector3.forward;

            _MovingPartGameobj.rotation = Quaternion.LookRotation(-sunWorldDirection, Vector3.up);
            _MovingPartGameobj.localEulerAngles = new Vector3(_MovingPartGameobj.localEulerAngles.x, 0, 0);

            BarricadeManager.ServerSetBarricadeTransform(MovingPart, _MovingPartGameobj.position, _MovingPartGameobj.rotation);
        }

        private void OnDestroy()
        {
            Plugin.OnTimeOfDayUpdated -= OnTimeOfDayUpdated;
        }
    }
}
