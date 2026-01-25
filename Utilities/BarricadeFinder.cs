using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace Wired
{
    public class BarricadeFinder
    {
        private readonly Vector3 _position;

        public BarricadeFinder(Vector3 position = new Vector3())
        {
            _position = position;
        }

        public List<BarricadeDrop> GetBarricadesInRadius(float radius = 0)
        {
            List<BarricadeDrop> result = new List<BarricadeDrop>();
            BarricadeRegion[,] regions = BarricadeManager.regions;
            if (radius == 0)
            {
                foreach (var region in regions)
                {
                    foreach (var drop in region.drops)
                    {
                        result.Add(drop);
                    }
                }
                return result;
            }
            foreach (var reg in regions)
            {
                foreach (var drop in reg.drops)
                {
                    float dist = Vector3.Distance(_position, drop.model.position);
                    if (dist < radius)
                    {
                        result.Add(drop);
                    }
                }
            }
            return result;
        }
        public List<BarricadeDrop> GetBarricadesOfType<T>() where T: Component
        {
            List<BarricadeDrop> result = new List<BarricadeDrop>();

            foreach(BarricadeRegion reg in BarricadeManager.regions)
            {
                foreach(BarricadeDrop drop in reg.drops)
                {
                    if (drop.model.TryGetComponent(out T _))
                        result.Add(drop);
                }
            }
            return result;
        }
    }
}