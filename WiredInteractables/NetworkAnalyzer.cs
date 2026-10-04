using SDG.Unturned;
using Steamworks;
using System;
using UnityEngine;
using Wired.Models;
using Wired.Utilities;
using Wired.WiredAssets;

namespace Wired.WiredInteractables;

internal class NetworkAnalyzer : MonoBehaviour, IWiredInteractable
{
    public Interactable interactable { get; private set; }
    public bool IsOn { get; private set; }
    private ConsumerNode _consumer;

    private InteractableSign _display;
    private InteractableSpot _spot;

    public void SetPowered(bool state)
    {
        if(_spot != null)
        {
            if(_spot.isPowered != state)
            {
                BarricadeManager.ServerSetSpotPowered(_spot, state);
            }
        }
        if(_display != null && state == false)
        {
            BarricadeManager.ServerSetSignText(_display, string.Empty);
        }
    }
    public void UpdateData(float supply, float demand)
    {
        if (_consumer == null) return;
        if (_consumer.IsPowered && _display != null)
        {
            BarricadeManager.ServerSetSignText(_display, $"{supply}<br>{demand}");
        }
    }

    private void Start()
    {
        if (!TryGetComponent(out InteractableSpot spot))
        {
            WiredLogger.Error($"No InteractableSpot in NetworkAnalyzer");
            Destroy(this);
            return;
        }
        interactable = spot;
        _spot = spot;
        _consumer = GetComponent<ConsumerNode>();

        if(_consumer.Asset is NetworkAnalyzerAsset naa)
        {
            var bar = new Barricade(Assets.find(EAssetType.ITEM, naa.DisplaySignID) as ItemBarricadeAsset);
            if (bar == null)
            {
                WiredLogger.Error($"Failed to find display barricade asset for Network Analyzer {naa.GUID}");
                Uninitialize();
            }

            var drop = BarricadeManager.dropNonPlantedBarricade(bar, transform.position, transform.rotation, 0, 0);
            if (drop == null)
            {
                WiredLogger.Error($"Failed to create display barricade for Network Analyzer {naa.GUID}");
                Uninitialize();
            }
            else
            {
                WiredLogger.Info($"Dropped display sign at {drop.position}, base position: {transform.position}");
            }

            _display = drop.GetComponent<InteractableSign>();
            if(_display == null)
            {
                WiredLogger.Error($"Network Analyzer {naa.GUID} has DisplayBarricadeID {naa.DisplaySignID}, but it's not a sign");
                Uninitialize();
            }
        }
    }

    public void Uninitialize()
    {
        if (_display != null)
        {
            BarricadeManager.tryGetRegion(_display.transform, out byte x, out byte y, out ushort plant, out _);
            BarricadeManager.destroyBarricade(BarricadeManager.FindBarricadeByRootTransform(_display.transform), x, y, plant);
        }
        Destroy(this);
    }
}
