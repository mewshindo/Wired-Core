using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wired.WiredInteractables;

namespace Wired.WiredAssets
{
    public class PlayerDetectorAsset : IWiredAsset
    {
        public Guid GUID { get; }
        public WiredAssetType Type => WiredAssetType.PlayerDetector;
        public float Radius { get; }
        public bool Inverted { get; }

        public PlayerDetectorAsset(Guid guid, float radius, bool inverted = false)
        {
            GUID = guid;
            Radius = radius;
            Inverted = inverted;
        }
    }
}
