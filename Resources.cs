using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired
{
    public class Resources
    {
        public EffectAsset node_consumer;
        public EffectAsset path_consumer;
        public EffectAsset preview_consumer;

        public EffectAsset node_power;
        public EffectAsset path_power;
        public EffectAsset preview_power;

        public EffectAsset node_gate;
        public EffectAsset path_gate;
        public EffectAsset preview_gate;

        public EffectAsset node_timer;
        public EffectAsset path_timer;
        public EffectAsset preview_timer;

        public EffectAsset wire_generic;

        public ItemBarricadeAsset generator_technical;

        public List<Guid> nodeeffects = new List<Guid>();
        public List<Guid> previeweffects = new List<Guid>();

        public Dictionary<Guid, WiredAsset> WiredAssets = new Dictionary<Guid, WiredAsset>();
        public Resources()
        {
            node_consumer = (EffectAsset)Assets.find(new Guid("ad1529d6692f473ead2ac79e70e273fb"));
            path_consumer = (EffectAsset)Assets.find(new Guid("0c3f255bcdb94ae0867de0c7de4d0f3e"));

            node_power = (EffectAsset)Assets.find(new Guid("f9f8409f96fe4624a280181523e5966d"));
            path_power = (EffectAsset)Assets.find(new Guid("aa4bf9e416b248f8b6ae4e48b30382a7"));

            node_gate = (EffectAsset)Assets.find(new Guid("8b7f38d937a0403eb99e97b535d9df83"));
            path_gate = (EffectAsset)Assets.find(new Guid("f8858fb1a24c4d70b1ca9a15e70606d5"));

            node_timer = (EffectAsset)Assets.find(new Guid("510beb5970b94441a248baed6e0d172d"));
            path_timer = (EffectAsset)Assets.find(new Guid("e0377b8351c945a797350447bd513a1e"));

            generator_technical = (ItemBarricadeAsset)Assets.find(new Guid("101d13181ef1407ca583686f36663a0f"));

            nodeeffects.Add(node_consumer.GUID);
            nodeeffects.Add(node_power.GUID);
            nodeeffects.Add(node_gate.GUID);
            nodeeffects.Add(path_consumer.GUID);
            nodeeffects.Add(path_power.GUID);
            nodeeffects.Add(path_gate.GUID);
            nodeeffects.Add(node_timer.GUID);
            nodeeffects.Add(path_timer.GUID);
        }
    }
    public enum WiredAssetType
    {
        WiringTool,
        RemoteTool,
        ManualTablet,
        Switch,
        Timer,
        RemoteReceiver,
        RemoteTransmitter
    }
    public class WiredAsset
    {
        public Guid GUID;
        public WiredAssetType Type;
        public WiredAsset(Guid guid, WiredAssetType type)
        {
            GUID = guid;
            Type = type;
        }
    }
}
