using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired
{
    public class WiredAsset
    {
        public WiredAsset(WiredAssetType type, string subtype) 
        {
            Type = type;
            Subtype = subtype;
        }
        public WiredAssetType Type { get; private set; }
        public string Subtype { get; private set; }
    }
}
