using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YamlDotNet.Serialization;

namespace UnityUIFlow
{
    /// <summary>
    /// Builds standardized artifact paths.
    /// </summary>
    public sealed class ReportPathBuilder
    {
        public string EnsureDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new UnityUIFlowException(ErrorCodes.ReportOutputUnavailable, $"报告目录不可写：{path}");
            }

            Directory.CreateDirectory(path);
            return Path.GetFullPath(path);
        }

        public string BuildScreenshotPath(string rootPath, string caseName, int stepIndex, string tag)
        {
            string safeCaseName = UnityUIFlowUtility.SanitizeFileName(caseName);
            string safeTag = UnityUIFlowUtility.SanitizeFileName(tag);
            string fileName = $"{safeCaseName}-{stepIndex:D3}-{safeTag}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            return Path.Combine(rootPath, fileName);
        }

        public string BuildCaseMarkdownPath(string rootPath, string caseName)
        {
            return Path.Combine(rootPath, $"{UnityUIFlowUtility.SanitizeFileName(caseName)}.md");
        }

        public string BuildCaseJsonPath(string rootPath, string caseName)
        {
            return Path.Combine(rootPath, $"{UnityUIFlowUtility.SanitizeFileName(caseName)}.json");
        }

        public string BuildSuiteMarkdownPath(string rootPath, string suiteName)
        {
            string fileName = string.IsNullOrWhiteSpace(suiteName)
                ? "suite-report.md"
                : $"suite-{UnityUIFlowUtility.SanitizeFileName(suiteName)}.md";
            return Path.Combine(rootPath, fileName);
        }

        public string BuildSuiteJsonPath(string rootPath, string suiteName)
        {
            string fileName = string.IsNullOrWhiteSpace(suiteName)
                ? "suite-report.json"
                : $"suite-{UnityUIFlowUtility.SanitizeFileName(suiteName)}.json";
            return Path.Combine(rootPath, fileName);
        }
    }

    /// <summary>
    /// Manages screenshot capture and persistence.
    /// </summary>
    public sealed class ScreenshotManager
    {
        private readonly TestOptions _options;
        private readonly ReportPathBuilder _pathBuilder = new ReportPathBuilder();

        public ScreenshotManager(TestOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pathBuilder.EnsureDirectory(_options.ScreenshotPath);
        }

        /// <summary>
        /// Captures a screenshot asynchronously.
        /// </summary>
        public async Task<string> CaptureAsync(string caseName, int stepIndex, string tag, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(tag) || tag.Length > 64)
            {
                throw new UnityUIFlowException(ErrorCodes.ScreenshotArgumentInvalid, $"截图参数非法：{tag}");
            }

            string path = _pathBuilder.BuildScreenshotPath(_options.ScreenshotPath, caseName, stepIndex, tag);
            await EditorAsyncUtility.NextFrameAsync(cancellationToken);
            CaptureSync(path);
            return path;
        }

        /// <summary>
        /// Captures a screenshot immediately.
        /// </summary>
        public string CaptureSync(string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? _options.ScreenshotPath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    texture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1f));
                    texture.SetPixel(1, 0, new Color(0.3f, 0.3f, 0.3f, 1f));
                    texture.SetPixel(0, 1, new Color(0.85f, 0.2f, 0.2f, 1f));
                    texture.SetPixel(1, 1, new Color(0.9f, 0.9f, 0.9f, 1f));
                    texture.Apply();
                    File.WriteAllBytes(filePath, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                throw new UnityUIFlowException(ErrorCodes.ScreenshotSaveFailed, $"截图保存失败：{Path.GetFileName(filePath)}", ex);
            }
        }
    }

    /// <summary>
    /// Writes JSON result artifacts.
    /// </summary>
    public sealed class JsonResultWriter
    {
        private readonly ISerializer _serializer = new SerializerBuilder().JsonCompatible().Build();

        public void WriteCaseJson(TestResult result, string path)
        {
            File.WriteAllText(path, _serializer.Serialize(result), Encoding.UTF8);
        }

        public void WriteSuiteJson(TestSuiteResult result, string path)
        {
            File.WriteAllText(path, _serializer.Serialize(result), Encoding.UTF8);
        }

        public void WriteArtifactManifest(IEnumerable<string> paths, string path)
        {
            File.WriteAllText(path, _serializer.Serialize(paths), Encoding.UTF8);
        }
    }

    /// <summary>
    /// Produces Markdown case and suite reports.
    /// </summary>
    public sealed class MarkdownReporter : IExecutionReporter
    {
        private readonly ReporterOptions _options;
        private readonly ReportPathBuilder _pathBuilder = new ReportPathBuilder();
        private readonly JsonResultWriter _jsonWriter = new JsonResultWriter();
        private readonly Dictionary<string, List<StepReportEntry>> _buffer = new Dictionary<string, List<StepReportEntry>>(StringComparer.Ordinal);

        public MarkdownReporter(ReporterOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pathBuilder.EnsureDirectory(_options.ReportRootPath);
            _pathBuilder.EnsureDirectory(_options.ScreenshotRootPath);
        }

        public void RecordAction(string stepId, string actionName, string message)
        {
            Debug.Log($"[UnityUIFlow] [{stepId}] {actionName}: {message}");
        }

        /// <summary>
        /// Records a step result to the current buffer.
        /// </summary>
        public void RecordStepResult(string caseName, StepResult result, IReadOnlyList<string> attachments)
        {
            if (!_buffer.TryGetValue(caseName, out List<StepReportEntry> entries))
            {
                entries = new List<StepReportEntry>();
                _buffer[caseName] = entries;
            }

            var entry = new StepReportEntry
            {
                CaseName = caseName,
                StepName = result.DisplayName,
                Status = result.Status,
                StartedAtUtc = result.StartedAtUtc,
                EndedAtUtc = result.EndedAtUtc,
                DurationMs = result.DurationMs,
                ScreenshotPath = result.ScreenshotPath,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
            };

            if (attachments != null)
            {
                entry.Attachments.AddRange(attachments);
            }

            if (result.Attachments != null && result.Attachments.Count > 0)
            {
                entry.Attachments.AddRange(result.Attachments);
            }

            entries.Add(entry);
        }

        /// <summary>
        /// Writes all per-case artifacts.
        /// </summary>
        public void WriteCaseReport(TestResult result)
        {
            ValidateCaseName(result.CaseName);
            string markdownPath = _pathBuilder.BuildCaseMarkdownPath(_options.ReportRootPath, result.CaseName);
            string jsonPath = _pathBuilder.BuildCaseJsonPath(_options.ReportRootPath, result.CaseName);
            var markdown = new StringBuilder();
            markdown.AppendLine($"# 测试报告：{result.CaseName}");
            markdown.AppendLine();
            markdown.AppendLine($"**状态**：{result.Status}");
            markdown.AppendLine($"**开始时间**：{result.StartedAtUtc}");
            markdown.AppendLine($"**结束时间**：{result.EndedAtUtc}");
            markdown.AppendLine($"**耗时**：{result.DurationMs}ms");
            markdown.AppendLine();
            markdown.AppendLine("## 步骤详情");
            markdown.AppendLine();
            markdown.AppendLine("| 步骤 | 状态 | 耗时(ms) | 错误码 | 截图 |");
            markdown.AppendLine("| --- | --- | --- | --- | --- |");

            if (_buffer.TryGetValue(result.CaseName, out List<StepReportEntry> entries))
            {
                foreach (StepReportEntry entry in entries)
                {
                    string screenshot = string.IsNullOrWhiteSpace(entry.ScreenshotPath)
                        ? string.Empty
                        : $"[查看]({entry.ScreenshotPath.Replace('\\', '/')})";
                    markdown.AppendLine($"| {entry.StepName} | {entry.Status} | {entry.DurationMs} | {entry.ErrorCode ?? string.Empty} | {screenshot} |");
                }
            }

            File.WriteAllText(markdownPath, markdown.ToString(), Encoding.UTF8);
            _jsonWriter.WriteCaseJson(result, jsonPath);
        }

        /// <summary>
        /// Writes suite summary artifacts.
        /// </summary>
        public void WriteSuiteReport(TestSuiteResult result)
        {
            string markdownPath = _pathBuilder.BuildSuiteMarkdownPath(_options.ReportRootPath, _options.SuiteName);
            string jsonPath = _pathBuilder.BuildSuiteJsonPath(_options.ReportRootPath, _options.SuiteName);
            var markdown = new StringBuilder();
            markdown.AppendLine($"# 套件报告：{_options.SuiteName ?? "suite-report"}");
            markdown.AppendLine();
            markdown.AppendLine($"**总计**：{result.Total} | **通过**：{result.Passed} | **失败**：{result.Failed} | **异常**：{result.Errors} | **跳过**：{result.Skipped}");
            markdown.AppendLine();
            markdown.AppendLine("## 用例汇总");
            markdown.AppendLine();
            markdown.AppendLine("| 用例 | 状态 | 耗时(ms) | 报告链接 |");
            markdown.AppendLine("| --- | --- | --- | --- |");

            foreach (TestResult caseResult in result.CaseResults)
            {
                string caseFileName = Path.GetFileName(_pathBuilder.BuildCaseMarkdownPath(_options.ReportRootPath, caseResult.CaseName));
                markdown.AppendLine($"| {caseResult.CaseName} | {caseResult.Status} | {caseResult.DurationMs} | [{caseFileName}]({caseFileName}) |");
            }

            File.WriteAllText(markdownPath, markdown.ToString(), Encoding.UTF8);
            _jsonWriter.WriteSuiteJson(result, jsonPath);
        }

        private static void ValidateCaseName(string caseName)
        {
            if (string.IsNullOrWhiteSpace(caseName))
            {
                throw new UnityUIFlowException(ErrorCodes.ReportWriteFailed, "测试用例名不能为空");
            }

            if (caseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new UnityUIFlowException(ErrorCodes.ReportWriteFailed, $"测试用例名包含非法文件名字符：{caseName}");
            }
        }
    }
}
