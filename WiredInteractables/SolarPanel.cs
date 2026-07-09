using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using UnityEngine;
using Wired.Models;
using Wired.Services;
using Wired.Utilities;
using Wired.WiredAssets;

namespace Wired.WiredInteractables;

public class SolarPanel : MonoBehaviour, IWiredInteractable
{
    public Interactable interactable { get; private set; }
    private SupplierNode _supplierNode;
    private SolarPanelAsset _asset;

    public bool IsOn { get; }

    public Vector3 PanelNormal;
    private Transform PanelNormalTransform;
    public Transform MovingPart;
    private Transform _MovingPartGameobj;

    private bool _movesToDefaultPosition;

    public void SetPowered(bool state)
    {

    }

    private void Start()
    {
        if (!TryGetComponent(out InteractableSpot spot))
        {
            Destroy(this);
            return;
        }
        interactable = spot;

        Plugin.OnTimeOfDayUpdated += OnTimeOfDayUpdated;
        BarricadeDrop.OnSalvageRequested_Global += OnSalvageRequested_Global;

        _supplierNode = gameObject.GetComponent<SupplierNode>();
        _asset = (SolarPanelAsset)_supplierNode.Asset;
        if(_supplierNode == null || _asset == null)
        {
            WiredLogger.Error($"Solar panel \"{BarricadeManager.FindBarricadeByRootTransform(this.transform).asset.FriendlyName}\" didn't initialize properly.");
            Uninitialize();
        }

        if (_asset.HasMovingPart)
        {
            _MovingPartGameobj = transform.Find("MovingPart");
        }
        else
        {
            var pn = transform.Find("PanelNormal");
            if (pn != null)
            {
                PanelNormal = pn.forward;
                PanelNormalTransform = pn;
            }
            else
            {
                WiredLogger.Error("PanelNormal GameObject not found");
            }
        }

        OnTimeOfDayUpdated(LightingManager.time, (float)LightingManager.time / (float)LightingManager.cycle);
    }

    private void OnSalvageRequested_Global(BarricadeDrop barricade, SteamPlayer instigatorClient, ref bool shouldAllow)
    {
        if (!_asset.HasMovingPart) return;
        if(barricade.model == MovingPart)
        {
            shouldAllow = false;
            BarricadeManager.tryGetRegion(this.transform, out byte x, out byte y, out ushort plant, out _);
            BarricadeManager.destroyBarricade(BarricadeManager.FindBarricadeByRootTransform(this.transform), x, y, plant);

            BarricadeManager.tryGetRegion(MovingPart, out x, out y, out plant, out _);
            BarricadeManager.destroyBarricade(BarricadeManager.FindBarricadeByRootTransform(MovingPart), x, y, plant);

            ItemTool.tryForceGiveItem(instigatorClient.player, (Assets.find(this._asset.GUID) as ItemAsset).id, 1);
        }
        else if(barricade.model == this.transform)
        {
            BarricadeManager.tryGetRegion(MovingPart, out byte x, out byte y, out ushort plant, out _);
            BarricadeManager.destroyBarricade(BarricadeManager.FindBarricadeByRootTransform(MovingPart), x, y, plant);
        }
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
        var bias = LevelLighting.bias;
        var truetime = (float)LightingManager.time / (float)LightingManager.cycle;
        float sunangle = Math.Abs((truetime / bias * 180f) / 1f - bias);

        if(_asset.HasMovingPart) RotateMovingPart(sunangle);

        var efficiency = GetSolarPanelEfficiency(sunangle);

        var newsupply = _asset.Supply * efficiency;
        _supplierNode.Supply = (float)Math.Round(newsupply);
        if(_supplierNode.Supply <= 0f && _supplierNode.IsPowered)
        {
            _supplierNode.SetPowered(false);
        }
        else if(_supplierNode.Supply > 0f && !_supplierNode.IsPowered)
        {
            _supplierNode.SetPowered(true);
        }

        NodeConnectionsService.RecalculatePowerForNode(_supplierNode);

        if (MovingPart == null) return;

        // WiredLogger.Info($"Current supply: {_supplierNode.Supply}");
    }

    private float GetSolarPanelEfficiency(float sunangle)
    {
        Quaternion sunRotation = Quaternion.Euler(-sunangle, LevelLighting.azimuth, 0f);
        Vector3 sunDirection = sunRotation * Vector3.forward;

        if(_asset.HasMovingPart)
        {
            PanelNormal = new Vector3(-MovingPart.forward.x, MovingPart.forward.y, -MovingPart.forward.z);
        }
        else
        {
            PanelNormal = PanelNormalTransform.forward;
        }

        float dot = Vector3.Dot(PanelNormal, sunDirection.normalized);

        // WiredLogger.Info($"Efficiency: {Mathf.Max(0f, dot)}, PanelNormal direction: {PanelNormal}, sunDirection: {sunDirection}");

        return Mathf.Max(0f, dot);
    }
    private void RotateMovingPart(float sunangle)
    {
        if(sunangle - 90f > _asset.MovingPartMaxAngle)
        {
            if(LightingManager.isNighttime)
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
    public void Uninitialize()
    {
        Plugin.OnTimeOfDayUpdated -= OnTimeOfDayUpdated;
        BarricadeDrop.OnSalvageRequested_Global -= OnSalvageRequested_Global;
        Destroy(this);
    }
}
