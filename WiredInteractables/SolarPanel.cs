using Rocket.Core.Assets;
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

    /// <summary>
    /// Exposed for <see cref="PlayerViewService.UpdateGogglesView"/> to put it into goggles ui.
    /// </summary>
    public float Efficiency { get; private set; }
    public bool IsSunBlocked { get; private set; }

    public Vector3 PanelNormal;
    private Transform PanelNormalTransform;
    public Transform MovingPart;
    private Transform _MovingPartGameobj;


    private float _currentPitch;
    private float _lastPitch;
    private float _movingPartTargetPitch;

    private float _lastBarricadeTransformCalled;

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

        var barricade = BarricadeManager.FindBarricadeByRootTransform(this.transform);
        if (_supplierNode == null || _asset == null)
        {
            WiredLogger.Error($"Solar panel \"{barricade.asset.FriendlyName}\" didn't initialize properly.");
            Uninitialize();
        }

        if (_asset.HasMovingPart)
        {
            _MovingPartGameobj = transform.Find("MovingPart");
            if (_MovingPartGameobj == null)
            {
                WiredLogger.Error($"MovingPart transform of \"{barricade.asset.FriendlyName}\" is missing.");
                return;
            }

            var bar = new Barricade(Assets.find(EAssetType.ITEM, _asset.MovingPartId) as ItemBarricadeAsset);
            if (bar == null)
            {
                WiredLogger.Error($"Couldn't find barricade asset for MovingPart of \"{barricade.asset.FriendlyName}\".");
                return;
            }

            Transform movingPartTransform = BarricadeManager.dropNonPlantedBarricade(
                bar,
                _MovingPartGameobj.position,
                barricade.model.rotation,
                barricade.GetServersideData().owner,
                barricade.GetServersideData().group
            );
            Console.WriteLine($"Moving part created at {movingPartTransform.position}, root position: {barricade.model.position}");

            MovingPart = movingPartTransform;
            PanelNormal = movingPartTransform.up;

            _currentPitch = _MovingPartGameobj.localEulerAngles.x;
            _movingPartTargetPitch = _currentPitch;
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
        if(Math.Abs(Mathf.DeltaAngle(_currentPitch, _movingPartTargetPitch)) < 1f)
        {
            return;
        }

        _currentPitch = Mathf.LerpAngle(_currentPitch, _movingPartTargetPitch, Time.deltaTime);

        _MovingPartGameobj.localEulerAngles = new Vector3(_currentPitch, 0, 0);

        MovingPart.position = _MovingPartGameobj.position;
        MovingPart.rotation = _MovingPartGameobj.rotation;

        _lastBarricadeTransformCalled = Time.realtimeSinceStartup;
        BarricadeManager.ServerSetBarricadeTransform(MovingPart, MovingPart.position, MovingPart.rotation);
        //if(Time.realtimeSinceStartup - _lastBarricadeTransformCalled > 0.1f)
        //{
        //}
    }

    private void OnTimeOfDayUpdated(uint timeOfDay, float timefraction)
    {
        var bias = LevelLighting.bias;
        var truetime = (float)LightingManager.time / (float)LightingManager.cycle;
        float sunangle = Math.Abs((truetime / bias * 180f) / 1f - bias);

        if(_asset.HasMovingPart) RotateMovingPart(sunangle);

        Efficiency = GetSolarPanelEfficiency(sunangle);

        var newsupply = _asset.Supply * Efficiency;
        _supplierNode.Supply = (float)Math.Round(newsupply);
        if(_supplierNode.Supply <= 0f)
        {
            _supplierNode.SetPowered(false);
        }
        else if(_supplierNode.Supply > 0f)
        {
            _supplierNode.SetPowered(true);
        }

        NodeConnectionsService.RecalculatePowerForNode(_supplierNode);

        // WiredLogger.Info($"Current supply: {_supplierNode.Supply}");
    }

    private float GetSolarPanelEfficiency(float sunangle)
    {
        Quaternion sunRotation = Quaternion.Euler(-sunangle, LevelLighting.azimuth, 0f);
        Vector3 sunDirection = sunRotation * Vector3.forward;

        if(_asset.HasMovingPart)
        {
            PanelNormal = new Vector3(-MovingPart.forward.x, MovingPart.forward.y, -MovingPart.forward.z);

            if (Physics.Raycast(MovingPart.position + MovingPart.forward, -(Quaternion.Euler(sunangle, LevelLighting.azimuth, 0f) * Vector3.forward), 256, RayMasks.BLOCK_COLLISION))
            {
                IsSunBlocked = true;
                return 0f;
            }
        }
        else
        {
            PanelNormal = PanelNormalTransform.forward;

            if (Physics.Raycast(PanelNormalTransform.position, -(Quaternion.Euler(sunangle, LevelLighting.azimuth, 0f) * Vector3.forward), 256, RayMasks.BLOCK_COLLISION))
            {
                IsSunBlocked = true;
                return 0f;
            }
        }

        IsSunBlocked = false;
        float dot = Math.Abs(Vector3.Dot(PanelNormal, sunDirection.normalized));

        if (LightingManager.isNighttime) dot *= _asset.NightSupplyModifier;
        // WiredLogger.Info($"Efficiency: {Mathf.Max(0f, dot)}, PanelNormal direction: {PanelNormal}, sunDirection: {sunDirection}");

        return Mathf.Max(0f, dot);
    }
    private void RotateMovingPart(float sunangle)
    {
        if(Math.Abs(sunangle - 90f) > _asset.MovingPartMaxAngle)
        {
            return;
        }

        Quaternion sunRotation = Quaternion.Euler(-sunangle, LevelLighting.azimuth, 0f);

        Vector3 sunWorldDirection = sunRotation * Vector3.forward;

        _MovingPartGameobj.rotation = Quaternion.LookRotation(-sunWorldDirection, Vector3.up);
        _movingPartTargetPitch = _MovingPartGameobj.localEulerAngles.x;

        _MovingPartGameobj.localEulerAngles = new Vector3(_currentPitch, 0, 0);

        // BarricadeManager.ServerSetBarricadeTransform(MovingPart, _MovingPartGameobj.position, _MovingPartGameobj.rotation);
    }
    public void Uninitialize()
    {
        Plugin.OnTimeOfDayUpdated -= OnTimeOfDayUpdated;
        BarricadeDrop.OnSalvageRequested_Global -= OnSalvageRequested_Global;
        if(MovingPart != null)
        {
            BarricadeManager.tryGetRegion(MovingPart, out byte x, out byte y, out ushort plant, out _);
            BarricadeManager.destroyBarricade(BarricadeManager.FindBarricadeByRootTransform(MovingPart), x, y, plant);
        }
        Destroy(this);
    }
}
