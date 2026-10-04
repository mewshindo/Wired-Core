using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.WiredAssets;
using Wired.WiredInteractables;

namespace Wired.Models;

/// <summary>
/// This gets attached to Input_# on logic gates.
/// </summary>
public class LogicGateSubnode : MonoBehaviour, IElectricNode
{
    public uint InstanceID => ParentNode.InstanceID;

    public IWiredAsset Asset { get; set; }

    public bool IsPowered { get; set; }

    public float Consumption { get; set; }

    public bool AllowPowerThrough { get; set; } = false;
    
    public bool IsBusy { get; set; }

    public Transform WireConnectPoint { get; set; }
    public BarricadeDrop barricade { get; set; }
    public LogicGate ParentNode { get; set; }
    private SphereCollider _col;

    public void SetPowered(bool powered)
    {
        IsPowered = powered;
        ParentNode.SetPowered(true);
    }

    private void Start()
    {
        WireConnectPoint = this.transform;
        IsPowered = false;
        _col = this.gameObject.AddComponent<SphereCollider>();
        _col.isTrigger = true;
        _col.radius = 0.1f;
    }

    public void Uninitialize()
    {
        Destroy(_col);
        Destroy(this);
    }
}
