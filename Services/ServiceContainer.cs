using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Wired.Services
{
    public class ServiceContainer
    {
        public WiredAssetsService WiredAssetsService { get; private set; }
        public NodeInitializationService NodeInitializationService { get; private set; }
        public NodeConnectionsService NodeConnectionsService { get; private set; }
        public WiringToolService WiringToolService { get; private set; }
        public RemoteToolService RemoteToolService { get; private set; }
        public PlayerViewService PlayerViewService { get; private set; }
        public KeypadUIService KeypadUIService { get; private set; }
        public WindService WindService { get; private set; }
        public JsonService JsonService { get; set; }
        public WiredDeltaService WiredDeltaService { get; set; }
        public ServiceContainer(Resources resources)
        {
            WiredAssetsService = new();
            NodeInitializationService = new(WiredAssetsService);
            NodeConnectionsService = new();
            WiringToolService = new (WiredAssetsService, NodeConnectionsService);
            RemoteToolService = new(WiredAssetsService);
            PlayerViewService = new GameObject("PlayerViewService").AddComponent<PlayerViewService>();
            PlayerViewService.Init(WiredAssetsService, resources, NodeConnectionsService, WiringToolService.SelectedNode);
            KeypadUIService = new();
            WindService = new(Plugin.Instance.Configuration.WindConfig.WindSpeedChangeRate, Plugin.Instance.Configuration.WindConfig.NoiseMapScale);
            JsonService = new(NodeConnectionsService, Path.Combine(Plugin.Instance.Directory, $"Nodes_{Provider.map}.json"));
            JsonService.LoadFromJson();
            WiredDeltaService = new GameObject("WiredDeltaService").AddComponent<WiredDeltaService>();
        }
    }
}
