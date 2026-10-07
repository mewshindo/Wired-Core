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
using Wired.WiredInteractables;

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
                WiredLogger.Info($"File found at {savepath}");
            }
            else
            {
                var directory = Path.GetDirectoryName(savepath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                _savepath = savepath;
                WiredLogger.Info($"Created file path at {_savepath}");
            }

            Provider.onCommenceShutdown += SaveToJson;
        }

        public void SaveToJson()
        {
            var nodes = _service.GetAllNodes();

            WiredLogger.Info($"Available nodes in service: {nodes.Count}");

            var reverseLookup = new Dictionary<IElectricNode, uint>();
            foreach (var kvp in nodes)
            {
                reverseLookup[kvp.Value] = kvp.Key;
            }

            var dataToSave = new List<SavedConnectionData>();

            foreach (var conn in _service.GetAllConnections())
            {
                if (reverseLookup.TryGetValue(conn.Node1, out uint id1) &&
                    reverseLookup.TryGetValue(conn.Node2, out uint id2))
                {
                    string additionalData = "";
                    if (conn.Node1 is LogicGateSubnode n1)
                    {
                        if (n1 == n1.ParentNode.Input0)
                            additionalData += "Node1_is_Input_0";
                        if (n1 == n1.ParentNode.Input1)
                            additionalData += "Node1_is_Input_1";
                    }
                    if (conn.Node2 is LogicGateSubnode n2)
                    {
                        if(n2 == n2.ParentNode.Input0)
                            additionalData += "Node2_is_Input_0";
                        if(n2 == n2.ParentNode.Input1)
                            additionalData += "Node2_is_Input_1";
                    }
                    dataToSave.Add(new SavedConnectionData
                    {
                        Node1ID = id1,
                        Node2ID = id2,
                        AdditionalData = additionalData
                    });
                }
            }

            string json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
            File.WriteAllText(_savepath, json);
            WiredLogger.Info($"Saved {dataToSave.Count} connections to {_savepath}");
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

            WiredLogger.Info($"Available nodes in service: {nodes.Count}");

            if (string.IsNullOrEmpty(_savepath) || !File.Exists(_savepath))
            {
                return;
            }

            string json = File.ReadAllText(_savepath);
            if (string.IsNullOrEmpty(json)) return;

            var loadedData = JsonConvert.DeserializeObject<List<SavedConnectionData>>(json);
            if (loadedData == null) return;

            int restoredCount = 0;


            bool errorsOccured = false;
            foreach (var data in loadedData)
            {
                bool hasNode1 = nodes.TryGetValue(data.Node1ID, out IElectricNode node1);
                bool hasNode2 = nodes.TryGetValue(data.Node2ID, out IElectricNode node2);

                if (hasNode1 && hasNode2)
                {
                    if (data.AdditionalData.Contains("Node1_is_Input_0"))
                    {
                        node1 = ((GateNode)node1).GetComponent<LogicGate>().Input0;
                    }
                    if (data.AdditionalData.Contains("Node1_is_Input_1"))
                    {
                        node1 = ((GateNode)node1).GetComponent<LogicGate>().Input1;
                    }
                    if (data.AdditionalData.Contains("Node2_is_Input_0"))
                    {
                        node2 = ((GateNode)node2).GetComponent<LogicGate>().Input0;
                    }
                    if (data.AdditionalData.Contains("Node2_is_Input_1"))
                    {
                        node2 = ((GateNode)node2).GetComponent<LogicGate>().Input1;
                    }

                    _service.LoadConnection(node1, node2);
                    restoredCount++;
                }
                else
                {
                    WiredLogger.Error($"Failed to connect {data.Node1ID} -> {data.Node2ID}. " +
                                      $"Found Node1? {hasNode1}, Found Node2? {hasNode2}");
                    errorsOccured = true;

                    if (nodes.Count > 0 && nodes.Count < 10)
                        WiredLogger.Info($"Available Keys: {string.Join(", ", nodes.Keys)}");
                }
            }

            WiredLogger.Info($"loadedData count: {loadedData.Count}");
            WiredLogger.Info($"Restored {restoredCount} connections from {_savepath}");

            if (errorsOccured)
            {
                var filename = $"Nodes-Backup-{DateTime.Now.Month}_{DateTime.Now.Day}__{DateTime.Now.ToString("HH:mm:ss")}.json";
                File.Copy(_savepath, Path.Combine(Plugin.Instance.Directory, filename));
                WiredLogger.Info($"Created Nodes backup file \"{filename}\"");
            }
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
        public string AdditionalData;
    }
}