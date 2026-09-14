using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Models;
using Wired.Services;
using Wired.Utilities;
using Wired.WiredAssets;

namespace Wired.WiredInteractables
{
    public class WindTurbine : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }

        public bool IsOn { get; private set; }

        public float Efficiency { get; private set; }

        private SupplierNode _supplierNode;
        private WindTurbineAsset _asset;

        private Transform _turbine;

        private void Start()
        {
            if (!TryGetComponent(out InteractableSpot spot))
            {
                Destroy(this);
                return;
            }
            interactable = spot;

            _supplierNode = gameObject.GetComponent<SupplierNode>();
            _asset = (WindTurbineAsset)_supplierNode.Asset;

            var barricade = BarricadeManager.FindBarricadeByRootTransform(this.transform);
            if (_supplierNode == null || _asset == null)
            {
                WiredLogger.Error($"Wind turbine \"{barricade.asset.FriendlyName}\" didn't initialize properly.");
                Uninitialize();
            }

            _turbine = transform.Find("Wind");
            if(_turbine == null)
            {
                _turbine = this.transform;
            }

            Plugin.OnTimeOfDayUpdated += OnTimeOfDayUpdated;
        }

        private void OnTimeOfDayUpdated(uint timeOfDay, float timefraction)
        {
            var wind = Plugin.Instance.Services.WindService.GetWindAt(transform.position);

            Efficiency = Math.Min(1, Math.Abs(Vector3.Dot(-_turbine.forward, wind.Direction)) * wind.Intensity * 2);
            WiredLogger.Info($"Efficiency: {Efficiency}, Forward: {-_turbine.forward}, Wind: {wind.Direction}, Dot: {Vector3.Dot(-_turbine.forward, wind.Direction)}");
            var newsupply = _asset.Supply * Efficiency;
            _supplierNode.Supply = (float)Math.Round(newsupply);
            if (_supplierNode.Supply <= 0f)
            {
                _supplierNode.SetPowered(false);
            }
            else if (_supplierNode.Supply > 0f)
            {
                _supplierNode.SetPowered(true);
            }

            NodeConnectionsService.RecalculatePowerForNode(_supplierNode);
        }

        public void SetPowered(bool state)
        {

        }

        public void Uninitialize()
        {
            Plugin.OnTimeOfDayUpdated -= OnTimeOfDayUpdated;
            Destroy(this);
        }
    }
}
