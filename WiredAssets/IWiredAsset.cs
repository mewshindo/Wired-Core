using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.WiredAssets
{
    public interface IWiredAsset
    {
        Guid GUID { get; }
        WiredAssetType Type { get; }
    }
    public enum WiredAssetType
    {
        WiringTool,
        RemoteTool,
        ManualTablet,
        Supplier,
        Consumer,
        Switch,
        Timer,
        RemoteReceiver,
        PlayerDetector,
        EngineerGoggles,
    }
}
