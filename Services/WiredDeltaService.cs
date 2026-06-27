using System;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Rocket.API;
using Rocket.Unturned.Chat;
using Wired.Utilities;
using SDG.Unturned;
using System.IO;
using System.Net.Http;
using UnityEngine;
using Newtonsoft.Json;
using System.Diagnostics;
using Wired.Models;

namespace Wired.Services
{
    public class WiredDeltaService : MonoBehaviour
    {
        private string _sessionId;

        private bool serviceActive = false;
        private int nextUpdate = 1;

        private ClientWebSocket _websocket;
        private CancellationTokenSource _cts;
        
        private static DateTime _lastCPUCheckTime = DateTime.UtcNow;
        private static TimeSpan _lastCpuTotalTime = Process.GetCurrentProcess().TotalProcessorTime;
        private static double _lastKnownCpu = 0.0;

        private float avgPowerRecalcTime1m = 0;
        private float avgPowerRecalcTime5m = 0;
        private float avgPowerRecalcTime15m = 0;

        private float avgCpuLoad1m = 0;
        private float avgCpuLoad5m = 0;
        private float avgCpuLoad15m = 0;

        private uint _powerRecalcCount = 0;
        public bool IsConnected()
        {
            return _websocket != null && _websocket.State == WebSocketState.Open;
        }
        public void Awake()
        {
            ElectricNetwork.PowerUpdated += OnPowerUpdated;
        }

        private void OnPowerUpdated(ElectricNetwork net, float tookMs)
        {
            _powerRecalcCount++;

            if (_powerRecalcCount == 1)
            {
                avgPowerRecalcTime1m = tookMs;
                avgPowerRecalcTime5m = tookMs;
                avgPowerRecalcTime15m = tookMs;
            }
            else
            {
                avgPowerRecalcTime1m = (avgPowerRecalcTime1m * 0.9f) + (tookMs * 0.1f);
                avgPowerRecalcTime5m = (avgPowerRecalcTime5m * 0.98f) + (tookMs * 0.02f);
                avgPowerRecalcTime15m = (avgPowerRecalcTime15m * 0.995f) + (tookMs * 0.005f);
            }
        }

        public void Update()
        {
            if (!serviceActive) return;

            if (Time.time >= nextUpdate)
            {
                if (IsConnected())
                {
                    nextUpdate = Mathf.FloorToInt(Time.time) + 1;
                    PerSecondUpdate();
                }
                else
                {
                    serviceActive = false;
                }
            }
        }
        private async void PerSecondUpdate()
        {
            if (!IsConnected()) return;
            
            await SendDataToWebSocket();

            _powerRecalcCount = 0;
        }
        public async void Connect(IRocketPlayer caller)
        {
            if (IsConnected())
            {
                UnturnedChat.Say(caller, "Already connected to Wired Delta.", UnityEngine.Color.yellow);
                WiredLogger.Warn("Attempted to connect to Wired Delta, but already connected.");
                return;
            }
            _sessionId = Guid.NewGuid().ToString();
            await Connect();
            if (IsConnected())
            {
                serviceActive = true;
                UnturnedChat.Say(caller, "Connected to Wired Delta!", UnityEngine.Color.green);
                WiredLogger.Info("Connected to Wired Delta with session ID: " + _sessionId);
            }
            else
            {
                UnturnedChat.Say(caller, "Failed to connect to Wired Delta.", UnityEngine.Color.red);
                WiredLogger.Error("Failed to connect to Wired Delta.");
            }
        }
        private async Task Connect()
        {
            _websocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            var uri = new Uri($"wss://localhost:7105/ws?sessionId={_sessionId}");

            await _websocket.ConnectAsync(uri, _cts.Token);


            string mapPath = Path.Combine(Level.info.path, "Map.png");

            if (!File.Exists(mapPath))
            {
                WiredLogger.Warn($"Map image not found at {mapPath}. Skipping map upload.");
                return;
            }
            byte[] imageBytes = File.ReadAllBytes(mapPath);

            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                var imageContent = new ByteArrayContent(imageBytes);

                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                content.Add(imageContent, "file", $"map.png");
                content.Add(new StringContent(_sessionId), "sessionId");

                await client.PostAsync("https://localhost:7105/api/map/upload", content);
            }
        }
        public async void Disconnect(IRocketPlayer caller)
        {
            await Disconnect();
            if (!IsConnected())
            {
                serviceActive = false;
                UnturnedChat.Say(caller, "Disconnected from Wired Delta.", UnityEngine.Color.green);
                WiredLogger.Info("Disconnected from Wired Delta.");
            }
            else
            {
                UnturnedChat.Say(caller, "Failed to disconnect from Wired Delta.", UnityEngine.Color.red);
                WiredLogger.Error("Failed to disconnect from Wired Delta.");
            }
        }
        private async Task Disconnect()
        {
            await _websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
            _websocket.Dispose();
            _websocket = null;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        private async Task SendDataToWebSocket()
        {
            var metrics = new
            {
                cpuLoad = GetCpuUsage(),
                powerRecalcs = _powerRecalcCount,
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                totalElectricNetworks = Plugin.Instance.Services.NodeConnectionsService.Networks.Count,
                avgCpuLoad1m,
                avgCpuLoad5m,
                avgCpuLoad15m,
                avgPowerRecalcTime1m,
                avgPowerRecalcTime5m,
                avgPowerRecalcTime15m
            };
            string jsonData = JsonConvert.SerializeObject(metrics);

            if (_websocket.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(jsonData);

            await _websocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cts.Token);
        }
        private double GetCpuUsage()
        {
            var currentProcess = Process.GetCurrentProcess();
            var currentTime = DateTime.UtcNow;
            double totalMsPassed = (currentTime - _lastCPUCheckTime).TotalMilliseconds;

            if(totalMsPassed < 1000)
            {
                return _lastKnownCpu;
            }

            currentProcess.Refresh();
            var currentTotalTime = currentProcess.TotalProcessorTime;

            double cpuUsedMs = (currentTotalTime - _lastCpuTotalTime).TotalMilliseconds;

            double cpuUsageTotal = (cpuUsedMs / (totalMsPassed * Environment.ProcessorCount)) * 100;

            _lastCPUCheckTime = currentTime;
            _lastCpuTotalTime = currentTotalTime;

            double finalPercentage = cpuUsageTotal * 100;

            if(finalPercentage < 0)
            {
                finalPercentage = 0;
            }
            else if (finalPercentage > 100)
            {
                finalPercentage = 100;
            }
            _lastKnownCpu = Math.Round(finalPercentage, 1);

            avgCpuLoad1m = (avgCpuLoad1m * 0.9f) + ((float)_lastKnownCpu * 0.1f);
            avgCpuLoad5m = (avgCpuLoad5m * 0.98f) + ((float)_lastKnownCpu * 0.02f);
            avgCpuLoad15m = (avgCpuLoad15m * 0.995f) + ((float)_lastKnownCpu * 0.005f);

            return _lastKnownCpu;
        }

    }
}
