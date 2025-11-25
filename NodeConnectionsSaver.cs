using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wired.Nodes;

namespace Wired
{
    public class NodeConnectionsSaver
    {
        private Dictionary<uint, IElectricNode> _nodes { get; set; } = new Dictionary<uint, IElectricNode>();
        private string _savepath;
        public NodeConnectionsSaver(Dictionary<uint, IElectricNode> nodes, string savepath)
        {
            _nodes = nodes;
            if(File.Exists(savepath))
            {
                _savepath = savepath;
                Console.WriteLine($"File found at {savepath}");
            }
            else
            {
                var directory = Path.GetDirectoryName(savepath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                Console.WriteLine($"Created a file at {directory}");
                _savepath = savepath;
            }
        }
        public Dictionary<uint, List<uint>> SaveConnections()
        {
            Dictionary<uint, List<uint>> connections = new Dictionary<uint, List<uint>>();
            foreach (var node in _nodes)
            {
                connections[node.Key] = node.Value.Connections.Select(n => n.instanceID).ToList();
            }
            return connections;
        }
        public void LoadConnections(Dictionary<uint, List<uint>> connections)
        {
            foreach (var node in _nodes)
            {
                node.Value.Connections.Clear();
                if (connections.ContainsKey(node.Key))
                {
                    foreach (var connID in connections[node.Key])
                    {
                        if (_nodes.ContainsKey(connID))
                        {
                            node.Value.AddConnection(_nodes[connID]);
                        }
                    }
                }
            }
        }
        public void SaveToJson()
        {
            var connections = SaveConnections();
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(connections, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(_savepath, json);
            Console.WriteLine($"Saved to {_savepath.ToString()}");
        }
        public void LoadFromJson()
        {
            if(_savepath == null)
                Console.WriteLine("savepath null???????????????????????????");
            Console.WriteLine($"Tried loading from {_savepath.ToString()}");
            if (!System.IO.File.Exists(_savepath))
                return;
            string json = System.IO.File.ReadAllText(_savepath);
            if(string.IsNullOrEmpty(json))
                return;
            var connections = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<uint, List<uint>>>(json);
            if(connections == null)
                return;
            LoadConnections(connections);
            Console.WriteLine($"Loaded {connections.Count} connections from {_savepath.ToString()}");
        }
    }
}
