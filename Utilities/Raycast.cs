using SDG.Unturned;
using UnityEngine;

namespace Wired
{
    public class Raycast
    {
        private Player player;
        private uint Range;

        public Raycast(Player player, uint range = 8)
        {
            this.player = player;
            Range = range;
        }
        public BarricadeDrop GetBarricade(out string colliderName, out float hitDistance)
        {
            colliderName = "";
            hitDistance = 0f;
            Transform aim = player.look.aim;

            if (!Physics.Raycast(aim.position, aim.forward, out var hit, Range, RayMasks.BARRICADE_INTERACT))
            {
                return null;
            }

            if (Physics.Raycast(aim.position, aim.forward, out var wallHit, hit.distance - 0.1f, RayMasks.BLOCK_COLLISION))
            {
                return null;
            }

            colliderName = hit.collider.name;
            Transform targetTransform = hit.transform;

            if (targetTransform.parent != null && targetTransform.parent.name == "Skeleton")
            {
                if (targetTransform.name.Contains("Hinge"))
                {
                    targetTransform = targetTransform.parent.parent;
                }
            }
            if(targetTransform.parent != null && targetTransform.name == "Clip")
            {
                targetTransform = targetTransform.parent;
            }

            hitDistance = hit.distance;
            return BarricadeManager.FindBarricadeByRootTransform(targetTransform);
        }
        public Vector3 GetPoint()
        {
            Transform aim = player.look.aim;
            if (!Physics.Raycast(aim.position, aim.forward, out var hitInfo, Range, RayMasks.BLOCK_COLLISION))
            {
                return Vector3.zero;
            }
            return hitInfo.point;
        }
    }
}
