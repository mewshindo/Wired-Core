using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Wired.Consumers
{
    public class VehicleBatteryCharger : CoolConsumer
    {
        private Collider _collider;

        private void Awake()
        {
            var ca = this.gameObject.transform.Find("Wired:ChargeArea");
            if (ca != null)
            {
                if(ca.GetComponent<Collider>() != null)
                {
                    _collider = ca.GetComponent<Collider>();
                    if (!_collider.isTrigger)
                    {
                        DebugLogger.LogError("Collider found on ChargeArea gameobject has isTrigger set to false!");
                    }
                }
                else
                {
                    DebugLogger.LogError("Collider not found on ChargeArea gameobject!");
                }
            }
            else
            {   
                _collider = this.gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)_collider).radius = 2f;
                if (this.gameObject.transform.Find("Wired:ChargeAreaCenter") != null)
                {
                    ((SphereCollider)_collider).center = gameObject.transform.Find("Wired:ChargeAreaCenter").position;
                }
            }
        }
    }
}
