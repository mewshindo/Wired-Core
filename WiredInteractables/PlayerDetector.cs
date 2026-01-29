using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Wired.Utilities;

namespace Wired.WiredInteractables
{
    /// <summary>
    /// ADD THIS CLASS ONTO 'Detector' GAMEOBJECT, NOT ONTO THE ROOT BARRICADE TRANSFORM!!!!!!!!!!!!!!!!!!!!
    /// </summary>
    public class PlayerDetector : MonoBehaviour, IWiredInteractable
    {
        public Interactable interactable { get; private set; }
        private Collider _collider;
        public bool IsOn {  get; private set; }

        // ----- Detector config -----
        public float Radius { get; set; }
        public bool Inverted { get; set; }
        public PlayerDetectorType DetectorType { get; set; }

        // ---------------------------

        public delegate void PlayerDetected(PlayerDetector detector);
        public static event PlayerDetected OnPlayerDetected;

        public delegate void PlayerUnDetected(PlayerDetector detector);
        public static event PlayerUnDetected OnPlayerUnDetected;
        public void SetPowered(bool state)
        {
            
        }

        private void Awake()
        {
            interactable = GetComponentInParent<InteractableSpot>();
            if (GetComponent<Collider>() != null)
            {
                _collider = GetComponent<Collider>();
                _collider.isTrigger = true;
            }
            else
            {
                _collider = gameObject.AddComponent<SphereCollider>();
                _collider.isTrigger = true;
                ((SphereCollider)_collider).radius = Radius;
            }
        }
        private void OnDestroy()
        {

        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                BarricadeManager.ServerSetSpotPowered((InteractableSpot)interactable, true);
                OnPlayerDetected?.Invoke(this);
            }
            else
            {
                WiredLogger.Log($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                BarricadeManager.ServerSetSpotPowered((InteractableSpot)interactable, false);
                OnPlayerUnDetected?.Invoke(this);
            }
            else
            {
                WiredLogger.Log($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            }
        }
    }
    public enum PlayerDetectorType
    {
        Sphere,
        Line,
    }
}
