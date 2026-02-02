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
        public PlayerViewService PlayerViewService { get; private set; }
        public JsonService JsonService { get; set; }
        public ServiceContainer(Resources resources)
        {
            WiredAssetsService = new WiredAssetsService();
            NodeInitializationService = new NodeInitializationService(WiredAssetsService);
            NodeConnectionsService = new NodeConnectionsService();
            WiringToolService = new WiringToolService(WiredAssetsService);
            PlayerViewService = new GameObject("PlayerViewService").AddComponent<PlayerViewService>();
            PlayerViewService.Init(WiredAssetsService, resources, NodeConnectionsService, WiringToolService.SelectedNode);
            JsonService = new JsonService(NodeConnectionsService, Path.Combine(Plugin.Instance.Directory, "Nodes.json"));
            JsonService.LoadFromJson();
        }
    }
}
