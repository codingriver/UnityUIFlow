// -----------------------------------------------------------------------
// UnityPilot Editor — MCP Server Process Manager
// Manages the external unitypilot MCP server process independently of Unity.
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace codingriver.unity.pilot
{
    internal struct McpServerStatus
    {
        public bool IsRunning;
        public bool HttpPortListening;
        public bool WsPortListening;
        public int? ProcessId;
        public string ProcessCommandLine;
        public string ErrorMessage;
        public int WsClientCount;
        public int HttpClientCount;
    }

    internal sealed class UnityPilotMcpServerManager
    {
        public static UnityPilotMcpServerManager Instance { get; } = new();

        private const int DefaultHttpPort = 8011;
        private const int DefaultWsPort = 8765;
        private const string DefaultPythonEntry = "./unitypilot/run_unitypilot_mcp.py";
        private const string DefaultLogLevel = "INFO";

        private static string HashSuffix => UnityPilotBridge.WsEndpointEditorPrefsKeySuffix;

        private static string HttpPortKey => $"UnityPilot.McpMgr.HttpPort.{HashSuffix}";
        private static string WsPortKey => $"UnityPilot.McpMgr.WsPort.{HashSuffix}";
        private static string PythonEntryKey => $"UnityPilot.McpMgr.PythonEntry.{HashSuffix}";
        private static string LogLevelKey => $"UnityPilot.McpMgr.LogLevel.{HashSuffix}";

        private int _httpPort = DefaultHttpPort;
        private int _wsPort = DefaultWsPort;
        private string _pythonEntryPath = DefaultPythonEntry;
        private string _logLevel = DefaultLogLevel;

        public int HttpPort { get => _httpPort; set { if (_httpPort != value) { _httpPort = value; SavePrefs(); } } }
        public int WsPort { get => _wsPort; set { if (_wsPort != value) { _wsPort = value; SavePrefs(); } } }
        public string PythonEntryPath => _pythonEntryPath;
        public string LogLevel { get => _logLevel; set { if (_logLevel != value) { _logLevel = value; SavePrefs(); } } }

        // ── Cached status (background refresh) ────────────────────────────────

        private const int RefreshIntervalMs = 2000;
        private readonly object _statusLock = new();
        private McpServerStatus _cachedStatus;
        private volatile bool _refreshRunning;
        private long _lastRefreshMs;

        private static readonly System.Net.Http.HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        private UnityPilotMcpServerManager()
        {
            LoadPrefs();
        }

        private void LoadPrefs()
        {
            _httpPort = EditorPrefs.GetInt(HttpPortKey, DefaultHttpPort);
            _wsPort = EditorPrefs.GetInt(WsPortKey, DefaultWsPort);
            _pythonEntryPath = EditorPrefs.GetString(PythonEntryKey, DefaultPythonEntry);
            _logLevel = EditorPrefs.GetString(LogLevelKey, DefaultLogLevel);
        }

        private void SavePrefs()
        {
            EditorPrefs.SetInt(HttpPortKey, _httpPort);
            EditorPrefs.SetInt(WsPortKey, _wsPort);
            EditorPrefs.SetString(PythonEntryKey, _pythonEntryPath ?? DefaultPythonEntry);
            EditorPrefs.SetString(LogLevelKey, _logLevel ?? DefaultLogLevel);
        }

        // ── Status ──────────────────────────────────────────────────────────

        public McpServerStatus GetStatus()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!_refreshRunning && (now - _lastRefreshMs > RefreshIntervalMs))
            {
                // Ensure the async state machine starts on a background thread,
                // so the main thread is never blocked by synchronous work before
                // the first await inside RefreshStatusAsync.
                Task.Run(RefreshStatusAsync);
            }
            lock (_statusLock) { return _cachedStatus; }
        }

        private async Task RefreshStatusAsync()
        {
            if (_refreshRunning) return;
            _refreshRunning = true;
            try
            {
                var status = new McpServerStatus();
                var httpTask = IsPortListeningAsync("127.0.0.1", _httpPort);
                var wsTask = IsPortListeningAsync("127.0.0.1", _wsPort);
                status.HttpPortListening = await httpTask;
                status.WsPortListening = await wsTask;
                status.IsRunning = status.HttpPortListening || status.WsPortListening;

                if (status.IsRunning)
                {
                    var (pid, cmdLine) = await Task.Run(() => FindMcpProcessByPorts());
                    if (pid.HasValue)
                    {
                        status.ProcessId = pid;
                        status.ProcessCommandLine = cmdLine;
                    }
                    var (wsCount, httpCount) = await FetchServerStatsAsync();
                    status.WsClientCount = wsCount;
                    status.HttpClientCount = httpCount;
                }

                lock (_statusLock) { _cachedStatus = status; }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityPilotMcpServerManager] Status refresh failed: {ex.Message}");
            }
            finally
            {
                _lastRefreshMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _refreshRunning = false;
            }
        }

        private async Task<(int wsCount, int httpCount)> FetchServerStatsAsync()
        {
            try
            {
                var url = $"http://127.0.0.1:{_httpPort}/stats";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (0, 0);
                var json = await response.Content.ReadAsStringAsync();
                int wsCount = ParseIntFromJson(json, "ws_connections");
                int httpCount = ParseIntFromJson(json, "http_sessions");
                return (wsCount, httpCount);
            }
            catch
            {
                return (0, 0);
            }
        }

        private static int ParseIntFromJson(string json, string key)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0;
            var pattern = $"\"{key}\"\\s*:\\s*(\\d+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success && int.TryParse(match.Groups[1].Value, out var val) ? val : 0;
        }

        // ── Start ───────────────────────────────────────────────────────────

        public void StartServer()
        {
            var status = GetStatus();
            if (status.IsRunning)
            {
                Debug.LogWarning("[UnityPilotMcpServerManager] MCP server already running.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

            try
            {
                StartViaDirectPython(projectRoot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityPilotMcpServerManager] Failed to start server: {ex.Message}");
            }
        }

        private void StartViaDirectPython(string projectRoot)
        {
            string entryFullPath = Path.IsPathRooted(_pythonEntryPath)
                ? _pythonEntryPath
                : Path.Combine(projectRoot, _pythonEntryPath);

            if (!File.Exists(entryFullPath))
            {
                Debug.LogError($"[UnityPilotMcpServerManager] Python entry not found: {entryFullPath}");
                return;
            }

            string logDir = Path.Combine(projectRoot, "log");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, "mcp-server.log");

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{entryFullPath}\" --transport http --http-port {_httpPort} --port {_wsPort} --log-file \"{logFile}\" --log-level {_logLevel}",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var proc = Process.Start(psi);
            Debug.Log($"[UnityPilotMcpServerManager] Started python process PID={proc?.Id} for {entryFullPath} (HTTP={_httpPort}, WS={_wsPort})");
        }

        // ── Stop ────────────────────────────────────────────────────────────

        public void StopServer()
        {
            var (pid, cmdLine) = FindMcpProcessByPorts();
            if (!pid.HasValue)
            {
                Debug.LogWarning("[UnityPilotMcpServerManager] No MCP server process found listening on configured ports.");
                return;
            }

            try
            {
                var proc = Process.GetProcessById(pid.Value);
                proc.Kill();
                Debug.Log($"[UnityPilotMcpServerManager] Killed MCP server process PID={pid.Value}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityPilotMcpServerManager] Failed to kill process PID={pid.Value}: {ex.Message}");
            }
        }

        // ── Port & Process Helpers ─────────────────────────────────────────

        private static async Task<bool> IsPortListeningAsync(string host, int port, int timeoutMs = 300)
        {
            try
            {
                // Run synchronous Connect on thread pool to avoid any potential
                // mono-runtime quirks with TcpClient.ConnectAsync on the main thread.
                using var client = new TcpClient();
                var connectTask = Task.Run(() =>
                {
                    try { client.Connect(host, port); return true; }
                    catch { return false; }
                });
                var timeoutTask = Task.Delay(timeoutMs);
                if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                    return false;
                return connectTask.Result;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPortListening(string host, int port)
        {
            try
            {
                using var client = new TcpClient();
                client.Connect(host, port);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private (int? pid, string cmdLine) FindMcpProcessByPorts()
        {
            var portsByPid = SafeGetListeningPortsByPid(out bool success);
            if (!success) return (null, null);

            // First try: look for PID listening on HTTP port
            foreach (var kv in portsByPid)
            {
                if (kv.Value.Contains(_httpPort) || kv.Value.Contains(_wsPort))
                {
                    string cmdLine = SafeGetCommandLine(kv.Key);
                    if (IsUnityPilotMcpLike(cmdLine))
                        return (kv.Key, cmdLine);
                }
            }

            // Second try: scan all python processes for command line match
            var p1 = Process.GetProcessesByName("python");
            var p2 = Process.GetProcessesByName("python3");
            foreach (var p in p1)
            {
                string cmdLine = SafeGetCommandLine(p.Id);
                if (IsUnityPilotMcpLike(cmdLine))
                    return (p.Id, cmdLine);
            }
            foreach (var p in p2)
            {
                string cmdLine = SafeGetCommandLine(p.Id);
                if (IsUnityPilotMcpLike(cmdLine))
                    return (p.Id, cmdLine);
            }

            return (null, null);
        }

        private static bool IsUnityPilotMcpLike(string cmdLine)
        {
            if (string.IsNullOrWhiteSpace(cmdLine)) return false;
            return cmdLine.IndexOf("run_unitypilot_mcp.py", StringComparison.OrdinalIgnoreCase) >= 0
                || cmdLine.IndexOf("unitypilot_mcp", StringComparison.OrdinalIgnoreCase) >= 0
                || cmdLine.IndexOf("unitypilot", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Dictionary<int, List<int>> SafeGetListeningPortsByPid(out bool success)
        {
            var result = new Dictionary<int, List<int>>();
            success = false;

#if UNITY_EDITOR_WIN
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano -p tcp",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var proc = Process.Start(psi);
                if (proc == null) return result;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(2000);

                if (string.IsNullOrWhiteSpace(output))
                    return result;

                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var raw in lines)
                {
                    var line = (raw ?? string.Empty).Trim();
                    if (!line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5)
                        continue;

                    var state = parts[3];
                    if (!state.Equals("LISTENING", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!int.TryParse(parts[4], out var pid) || pid <= 0)
                        continue;

                    int port = SafeParsePortFromEndpoint(parts[1]);
                    if (port <= 0)
                        continue;

                    if (!result.TryGetValue(pid, out var list))
                    {
                        list = new List<int>();
                        result[pid] = list;
                    }
                    if (!list.Contains(port))
                        list.Add(port);
                }

                success = true;
            }
            catch
            {
                success = false;
            }
#endif
            return result;
        }

        private static int SafeParsePortFromEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return -1;

            int idx = endpoint.LastIndexOf(':');
            if (idx < 0 || idx >= endpoint.Length - 1)
                return -1;

            var portText = endpoint.Substring(idx + 1).Trim();
            return int.TryParse(portText, out var port) ? port : -1;
        }

        private static string SafeGetCommandLine(int pid)
        {
#if UNITY_EDITOR_WIN
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = $"process where ProcessId={pid} get CommandLine /value",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var proc = Process.Start(psi);
                if (proc == null) return "(读取命令行失败)";
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(1000);

                if (string.IsNullOrWhiteSpace(output)) return "(空)";
                var marker = "CommandLine=";
                var idx = output.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return output.Trim();

                var cmd = output.Substring(idx + marker.Length).Trim();
                return string.IsNullOrWhiteSpace(cmd) ? "(空)" : cmd;
            }
            catch
            {
                return "(读取命令行失败)";
            }
#else
            return "(当前平台未实现命令行读取)";
#endif
        }
    }
}
