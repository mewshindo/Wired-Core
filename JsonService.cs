using Newtonsoft.Json;
using SDG.Provider.Services;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Wired.Models;
using Wired.Utilities;

namespace Wired.Services
{
    public class JsonService
    {
        private readonly string _savepath;
        private readonly NodeConnectionsService _service;

        public JsonService(NodeConnectionsService service, string savepath)
        {
            _service = service;

            if (File.Exists(savepath))
            {
                _savepath = savepath;
                WiredLogger.Log($"File found at {savepath}");
            }
            else
            {
                var directory = Path.GetDirectoryName(savepath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                _savepath = savepath;
                WiredLogger.Log($"Created file path at {_savepath}");
            }

            Provider.onCommenceShutdown += SaveToJson;
        }

        public void SaveToJson()
        {
            var nodes = _service.GetAllNodes();

            WiredLogger.Log($"Available nodes in service: {nodes.Count}");

            var reverseLookup = new Dictionary<IElectricNode, uint>();
            foreach (var kvp in nodes)
            {
                reverseLookup[kvp.Value] = kvp.Key;
            }

            var allConnections = _service.GetAllConnections();
            var dataToSave = new List<SavedConnectionData>();

            foreach (var conn in allConnections)
            {
                if (reverseLookup.TryGetValue(conn.Node1, out uint id1) &&
                    reverseLookup.TryGetValue(conn.Node2, out uint id2))
                {
                    dataToSave.Add(new SavedConnectionData
                    {
                        Node1ID = id1,
                        Node2ID = id2,
                        WirePath = conn.WirePath.Select(v => new float[] { v.x, v.y, v.z }).ToList()
                    });
                }
            }

            string json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
            File.WriteAllText(_savepath, json);
            WiredLogger.Log($"Saved {dataToSave.Count} connections to {_savepath}");
        }

        public void LoadFromJson()
        {
            var bf = new BarricadeFinder();
            var nodes = new Dictionary<uint, IElectricNode>();
            foreach(var bar in bf.GetBarricadesInRadius())
            {
                if(bar.model.TryGetComponent(out IElectricNode node))
                {
                    nodes.Add(bar.instanceID, node);
                }
            }

            WiredLogger.Log($"Available nodes in service: {nodes.Count}");

            if (string.IsNullOrEmpty(_savepath) || !File.Exists(_savepath))
            {
                WiredLogger.Error("savepath null???????????????????????????");
                return;
            }

            string json = File.ReadAllText(_savepath);
            if (string.IsNullOrEmpty(json)) return;

            var loadedData = JsonConvert.DeserializeObject<List<SavedConnectionData>>(json);
            if (loadedData == null) return;

            int restoredCount = 0;

            foreach (var data in loadedData)
            {
                bool hasNode1 = nodes.TryGetValue(data.Node1ID, out IElectricNode node1);
                bool hasNode2 = nodes.TryGetValue(data.Node2ID, out IElectricNode node2);

                if (hasNode1 && hasNode2)
                {
                    List<Vector3> path = data.WirePath?
                        .Select(arr => new Vector3(arr[0], arr[1], arr[2]))
                        .ToList() ?? new List<Vector3>();

                    _service.LoadConnection(node1, node2, path);
                    restoredCount++;
                }
                else
                {
                    WiredLogger.Error($"Failed to connect {data.Node1ID} -> {data.Node2ID}. " +
                                      $"Found Node1? {hasNode1}, Found Node2? {hasNode2}");

                    if (nodes.Count > 0 && nodes.Count < 10)
                        WiredLogger.Log($"Available Keys: {string.Join(", ", nodes.Keys)}");
                }
            }

            WiredLogger.Log($"loadedData count: {loadedData.Count}");
            WiredLogger.Log($"Restored {restoredCount} connections from {_savepath}");
        }
    }
    [Serializable]
    public class SavedConnectionData
    {
        [SerializeField]
        public uint Node1ID;
        [SerializeField]
        public uint Node2ID;
        [SerializeField]
        public List<float[]> WirePath;
    }
}