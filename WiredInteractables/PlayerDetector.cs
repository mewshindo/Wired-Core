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

    private InteractableSpot _interactable;
    private GateNode _switchNode;
    private Collider _collider;

    private bool _state;

    private readonly HashSet<Collider> _knownColliders = new();

    private void Awake()
    {
        _interactable = GetComponentInParent<InteractableSpot>();
        _switchNode = GetComponentInParent<GateNode>();

        if (_switchNode == null)
        {
            WiredLogger.Error("PlayerDetector couldn't find a switch node");
            Destroy(gameObject);
            return;
        }

        if (_interactable == null)
        {
            WiredLogger.Error("PlayerDetector couldn't find an interactable");
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
        Plugin.OnTimeOfDayUpdated += OnTimeOfDayUpdated;
    }

    private void OnTimeOfDayUpdated(uint timeOfDay, float timefraction)
    {
        var removed = _knownColliders.RemoveWhere(c => c == null || !_collider.bounds.Intersects(c.bounds));
        if (removed > 0 && _knownColliders.Count == 0)
            UnDetect();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            WiredLogger.Info($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            return;
        }

        if (_knownColliders.Add(other))
            Detect();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            WiredLogger.Info($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            return;
        }

        if (_knownColliders.Remove(other) && _knownColliders.Count == 0)
            UnDetect();
    }

    private void HandlePlayerStanceChanged(PlayerStance stance)
    {
        var controller = stance.player.movement.controller;
        bool intersects = _collider.bounds.Intersects(controller.bounds);
        bool known = _knownColliders.Contains(controller);

        if (intersects && !known)
        {
            _knownColliders.Add(controller);
            Detect();
        }
        else if (!intersects && known)
        {
            _knownColliders.Remove(controller);
            if (_knownColliders.Count == 0)
                UnDetect();
        }
    }

    private void Detect()
    {
        if(_state == true) return;
        _state = true;
        BarricadeManager.ServerSetSpotPowered(_interactable, true);
        _switchNode.Switch(!Inverted);
        OnPlayerDetected?.Invoke(this);
    }

    private void UnDetect()
    {
        if(_state == false) return;
        _state = false;
        BarricadeManager.ServerSetSpotPowered(_interactable, false);
        _switchNode.Switch(Inverted);
        OnPlayerUnDetected?.Invoke(this);
    }

    public void Uninitialize()
    {
        Plugin.OnPlayerStanceChanged -= HandlePlayerStanceChanged;
        Plugin.OnTimeOfDayUpdated -= OnTimeOfDayUpdated;
        if (_collider != null) Destroy(_collider);
        Destroy(this);
    }
}
