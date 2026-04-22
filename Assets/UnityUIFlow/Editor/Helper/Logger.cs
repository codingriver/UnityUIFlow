// -----------------------------------------------------------------------
// UnityPilot Editor — https://github.com/codingriver/unitypilot
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityUIFlow
{
    /// <summary>
    /// 每次打开 Unity 编辑器（新会话）首次写入前会清空并写入会话头；同一会话内脚本域重载不清空，继续追加。
    /// 所有写入均立即落盘（无缓冲），时间戳精确到毫秒（本地时区）。
    /// 支持标签、主线程帧号检测。
    /// </summary>
    [InitializeOnLoad]
    internal static class Logger
    {
        private static readonly object FileLock = new object();
        private static readonly int MainThreadId;

        public static string LogFilePath = "./log/UnityUIFlow.log";

        /// <summary>
        /// 当前线程是否为主线程。
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        static Logger()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;

            var dir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        // ── 核心写入 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 将一行文本追加到日志文件。自动在行首添加毫秒级本地时间戳、线程/帧号、标签，立即落盘。
        /// </summary>
        /// <param name="line">日志正文（通常已包含 [LEVEL] message）。</param>
        /// <param name="tags">可选标签列表，会追加在行尾。</param>
        public static void AppendLine(string line, params string[] tags)
        {
            if (string.IsNullOrEmpty(line)) return;

            var sb = new StringBuilder();
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]");

            if (IsMainThread)
            {
                sb.Append($" [frame:{Time.frameCount}]");
            }
            else
            {
                sb.Append($" [thread:{Thread.CurrentThread.ManagedThreadId}]");
            }

            sb.Append($" {line}");

            if (tags != null && tags.Length > 0)
            {
                sb.Append($" [{string.Join(",", tags)}]");
            }

            lock (FileLock)
            {
                try
                {
                    File.AppendAllText(LogFilePath, sb.ToString() + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // 避免日志失败反噬编辑器
                }
            }
        }

        // ── 带标签的写入接口 ──────────────────────────────────────────────────

        /// <summary>调试日志（带标签）。格式：[timestamp] [frame] [DEBUG] message [tags]</summary>
        public static void LogDebug(string message, params string[] tags) =>
            AppendLine($"[DEBUG] {message}", tags);

        /// <summary>普通信息日志（带标签）。格式：[timestamp] [frame] [INFO ] message [tags]</summary>
        public static void Log(string message, params string[] tags) =>
            AppendLine($"[INFO ] {message}", tags);

        /// <summary>警告日志（带标签）。格式：[timestamp] [frame] [WARN ] message [tags]</summary>
        public static void LogWarning(string message, params string[] tags) =>
            AppendLine($"[WARN ] {message}", tags);

        /// <summary>错误日志（带标签）。格式：[timestamp] [frame] [ERROR] message [tags]</summary>
        public static void LogError(string message, params string[] tags) =>
            AppendLine($"[ERROR] {message}", tags);

        /// <summary>异常日志（带标签）。</summary>
        public static void LogException(Exception ex, params string[] tags) =>
            AppendLine($"[ERROR] Exception: {ex}", tags);

        // ── 网络通信日志 ───────────────────────────────────────────────────────

        public static void LogNetSend(string message, params string[] tags) =>
            AppendLine($"[INFO ] [NET] [SEND] {message}", tags);

        public static void LogNetRecv(string message, params string[] tags) =>
            AppendLine($"[INFO ] [NET] [RECV] {message}", tags);

        // ── 工具方法 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 截断超长 JSON payload，避免单条日志过大。
        /// </summary>
        public static string TruncatePayload(string json, int maxLen = 800)
        {
            if (string.IsNullOrEmpty(json) || json.Length <= maxLen) return json;
            return json.Substring(0, maxLen) + $" ... [truncated, total={json.Length}]";
        }
    }
}
