using SDG.Unturned;
using UnityEngine;

namespace Wired
{
    public class Raycast
    {
        private Player player;

        public Raycast(Player player)
        {
            this.player = player;
        }
        public BarricadeDrop GetBarricade(out string collider)
        {
            Transform aim = player.look.aim;
            collider = "";
            if (!Physics.Raycast(aim.position, aim.forward, out var hitInfo, float.PositiveInfinity, RayMasks.BLOCK_COLLISION))
            {
                return null;
            }
            if (!Physics.Raycast(aim.position, aim.forward, out var hit, float.PositiveInfinity, RayMasks.BARRICADE_INTERACT) || hitInfo.transform != hit.transform)
            {
                return null;
            }
            RaycastHit raycastInfo;
            Physics.Raycast(new Ray(player.look.aim.position, player.look.aim.forward), out raycastInfo, float.PositiveInfinity, RayMasks.BARRICADE_INTERACT);
            if ((raycastInfo.transform.name == "Hinge" || raycastInfo.transform.name == "Left_Hinge" || raycastInfo.transform.name == "Right_Hinge") && raycastInfo.transform.parent.name == "Skeleton")
            {
                BarricadeDrop barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(raycastInfo.transform.parent.parent);
                collider = raycastInfo.collider.name;
                return barricadeDrop;
            }
            BarricadeDrop bardrop = BarricadeManager.FindBarricadeByRootTransform(raycastInfo.transform);
            collider = raycastInfo.collider.name;
            return bardrop;
        }
        public Vector3 GetPoint(float range = float.PositiveInfinity)
        {
            Transform aim = player.look.aim;
            if (!Physics.Raycast(aim.position, aim.forward, out var hitInfo, range, RayMasks.BLOCK_COLLISION))
            {
                return Vector3.zero;
            }
            return hitInfo.point;
        }
    }
}
