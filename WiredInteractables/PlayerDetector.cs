using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Models;
using Wired.Utilities;

namespace Wired.WiredInteractables;

/// <summary>
/// ADD THIS CLASS ONTO 'Detector' GAMEOBJECT, NOT ONTO THE ROOT BARRICADE TRANSFORM!!!!!!!!!!!!!!!!!!!!
/// </summary>
public class PlayerDetector : MonoBehaviour
{
    public float Radius = 1f;
    public bool Inverted;

    public event Action<PlayerDetector> OnPlayerDetected;
    public event Action<PlayerDetector> OnPlayerUnDetected;

    private InteractableSpot interactable;
    private GateNode _switchNode;
    private Collider _collider;

    private readonly HashSet<Collider> knownColliders = new();

    private void Awake()
    {
        interactable = GetComponentInParent<InteractableSpot>();
        _switchNode = GetComponentInParent<GateNode>();

        if (_switchNode == null)
        {
            WiredLogger.Error("PlayerDetector couldn't find a switch node");
            Destroy(gameObject);
            return;
        }

        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = Radius;
            _collider = sphere;
        }
        _collider.isTrigger = true;

        _collider.gameObject.layer = 30;
        _collider.gameObject.tag = "Trap";

        Plugin.OnPlayerStanceChanged += HandlePlayerStanceChanged;
    }

    private void OnDestroy()
    {
        Plugin.OnPlayerStanceChanged -= HandlePlayerStanceChanged;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            WiredLogger.Info($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            return;
        }

        if (knownColliders.Add(other))
            Detect();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            WiredLogger.Info($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            return;
        }

        if (knownColliders.Remove(other) && knownColliders.Count == 0)
            UnDetect();
    }

    private void HandlePlayerStanceChanged(PlayerStance stance)
    {
        var controller = stance.player.movement.controller;
        bool intersects = _collider.bounds.Intersects(controller.bounds);
        bool known = knownColliders.Contains(controller);

        if (intersects && !known)
        {
            knownColliders.Add(controller);
            Detect();
        }
        else if (!intersects && known)
        {
            knownColliders.Remove(controller);
            if (knownColliders.Count == 0)
                UnDetect();
        }
    }

    private void Detect()
    {
        BarricadeManager.ServerSetSpotPowered(interactable, true);
        _switchNode.Switch(!Inverted);
        OnPlayerDetected?.Invoke(this);
    }

    private void UnDetect()
    {
        BarricadeManager.ServerSetSpotPowered(interactable, false);
        _switchNode.Switch(Inverted);
        OnPlayerUnDetected?.Invoke(this);
    }

    public void Uninitialize()
    {
        Plugin.OnPlayerStanceChanged -= HandlePlayerStanceChanged;
        if (_collider != null) Destroy(_collider);
        Destroy(this);
    }
}
