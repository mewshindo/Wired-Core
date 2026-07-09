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

namespace Wired.WiredInteractables;

public class Battery : MonoBehaviour, IWiredInteractable
{
    public Interactable interactable { get; private set; }
    public bool IsOn { get; }
    public BatteryState State { get; private set; }

    public float Charge { get; private set; }
    public float MaxCapacity { get; private set; }
    public float MaxBurnPerSecond { get; private set; }
    public float Supply {  get; private set; }

    private float _currentBurn;

    private SupplierNode _supplierNode;
    private BatteryAsset _asset;

    private void Start()
    {
        _supplierNode = GetComponent<SupplierNode>();
        _asset = (BatteryAsset)_supplierNode.Asset;
        interactable = GetComponent<Interactable>();

        Plugin.OnTimeOfDayUpdated += OnTimeOfDayUpdated;

        if (_supplierNode == null || _asset == null)
        {
            WiredLogger.Error($"Battery \"{BarricadeManager.FindBarricadeByRootTransform(this.transform).asset.FriendlyName}\" didn't initialize properly.");
            Uninitialize();
        }

        MaxCapacity = _asset.Capacity;
        MaxBurnPerSecond = _asset.MaxBurnPerSecond * 10;
        Supply = _asset.Supply;
        Charge = MaxCapacity;
    }

    private void OnTimeOfDayUpdated(uint timeOfDay, float timefraction)
    {
        switch (State)
        {
            case BatteryState.Idle:
                break;
            case BatteryState.Charging:
                Charge = Charge + _currentBurn >= MaxCapacity ? MaxCapacity : Charge + _currentBurn;
                break;
            case BatteryState.Discharging:
                Charge = Charge - _currentBurn <= 0 ? 0 : Charge - _currentBurn;
                break;
        }
        if(Charge <= 0 && State != BatteryState.Idle)
        {
            NodeConnectionsService.RecalculatePowerForNode(_supplierNode);
            State = BatteryState.Idle;
            _supplierNode.Supply = 0f;
        }
        else if (Charge > 0 && State == BatteryState.Idle)
        {
            _supplierNode.Supply = _asset.Supply;
            NodeConnectionsService.RecalculatePowerForNode(_supplierNode);
        }
        else
        {
            _supplierNode.Supply = _asset.Supply;
        }

        if(interactable is InteractableSpot spot)
        {
            if (Charge > 0)
            {
                if (!spot.isPowered)
                {
                    BarricadeManager.ServerSetSpotPowered(spot, true);
                }
            }
            else
            {
                if (spot.isPowered)
                {
                    BarricadeManager.ServerSetSpotPowered(spot, false);
                }
            }
        }
    }

    public void SetPowered(bool state)
    {

    }
    public void SetState(BatteryState state, float modifier)
    {
        State = state;
        _currentBurn = MaxBurnPerSecond * modifier;
    }

    public void Uninitialize()
    {
        Plugin.OnTimeOfDayUpdated -= OnTimeOfDayUpdated;
        Destroy(this);
    }
}

public enum BatteryState
{
    Idle,
    Charging,
    Discharging
}
