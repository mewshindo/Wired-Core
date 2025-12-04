using SDG.Unturned;
using System;
using UnityEngine;
using Wired.Nodes;

namespace Wired
{
    public class PlayerDetector : MonoBehaviour
    {
        private Collider _collider;
        public float Radius = 2f;
        public void Awake()
        {
            if(GetComponent<Collider>() != null)
            {
                DebugLogger.Log($"Collider found on this playerdetector");
                _collider = GetComponent<Collider>();
                _collider.isTrigger = true;
            }
            else
            {
                DebugLogger.Log($"Collider not found on this playerdetector, creating a sphere with radius {Radius}");
                _collider = gameObject.AddComponent<SphereCollider>();
                _collider.isTrigger = true;
                ((SphereCollider)_collider).radius = Radius;
            }
        }
        public void OnDestroy()
        {
            DebugLogger.Log($"Destroyed playerdetector");
            Destroy(this);
        }
        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                BarricadeManager.ServerSetSpotPowered(gameObject.GetComponentInParent<InteractableSpot>(), true);
                Console.WriteLine($"Found gatenode in parent: {gameObject.GetComponentInParent<GateNode>() != null}");
                Console.WriteLine($"State: {gameObject.GetComponentInParent<GateNode>().IsOpen}");
                
                gameObject.GetComponentInParent<GateNode>().Toggle(true);
                Console.WriteLine($"Toggled, new state: {gameObject.GetComponentInParent<GateNode>().IsOpen} ");
            }
            else
            {
                Console.WriteLine($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                BarricadeManager.ServerSetSpotPowered(gameObject.GetComponentInParent<InteractableSpot>(), false);
                Console.WriteLine($"Found gatenode in parent: {gameObject.GetComponentInParent<GateNode>() != null}");
                Console.WriteLine($"State: {gameObject.GetComponentInParent<GateNode>().IsOpen}");
                
                gameObject.GetComponentInParent<GateNode>().Toggle(false);
                Console.WriteLine($"Toggled, new state: {gameObject.GetComponentInParent<GateNode>().IsOpen} ");
            }
            else
            {
                Console.WriteLine($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            }
        }
    }
}
