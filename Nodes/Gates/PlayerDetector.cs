using SDG.Unturned;
using System;
using UnityEngine;
using Wired.Nodes;

namespace Wired.Nodes
{
    public class PlayerDetector : SwitchNode
    {
        private SphereCollider _collider;
        private Rigidbody _rigidbody;
        private float _radius = 2f;
        public PlayerDetector(float radius = 95.0f)
        {
            _radius = radius;
        }
        protected override void Awake()
        {
            base.Awake();

            _collider = gameObject.AddComponent<SphereCollider>();
            _collider.isTrigger = true;
            _collider.radius = 2f;

            Console.WriteLine($"Initialized a playerdetector with radius {_collider.radius}");
        }
        private void OnDestroy()
        {
            if (_collider != null)
            {
                GameObject.Destroy(_collider);
                Destroy(this);
            }
        }
        public override void unInit()
        {
            base.unInit();
            Console.WriteLine($"Destroyed a playerdetector");
            Destroy(gameObject);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                BarricadeManager.ServerSetSpotPowered(gameObject.GetComponent<InteractableSpot>(), true);
                gameObject.GetComponent<SwitchNode>().Toggle(true);
                Console.WriteLine($"Detected player {other.gameObject.name}");
            }
            else
            {
                Console.WriteLine($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                BarricadeManager.ServerSetSpotPowered(gameObject.GetComponent<InteractableSpot>(), false);
                gameObject.GetComponent<SwitchNode>().Toggle(false);
                Console.WriteLine($"Player {other.gameObject.name} left detection area");
            }
            else
            {
                Console.WriteLine($"Ignored object {other.gameObject.name} with tag {other.gameObject.tag}");
            }
        }
    }
}
