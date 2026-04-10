using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace UnityUIFlow
{
    /// <summary>
    /// Loads CLI defaults from a JSON config file.
    /// </summary>
    public sealed class ConfigFileLoader
    {
        private readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

        /// <summary>
        /// Loads config file values.
        /// </summary>
        public Dictionary<string, object> Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                object raw = _deserializer.Deserialize<object>(File.ReadAllText(path));
                return YamlObjectReader.AsMap(raw, path);
            }
            catch (Exception ex)
            {
                throw new UnityUIFlowException(ErrorCodes.CliConfigFileInvalid, $"配置文件解析失败：{path}，{ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Parses UnityUIFlow command-line parameters.
    /// </summary>
    public sealed class CommandLineOptionsParser
    {
        private readonly ConfigFileLoader _configFileLoader = new ConfigFileLoader();

        /// <summary>
        /// Parses current process args.
        /// </summary>
        public CliOptions Parse(string[] args = null)
        {
            args ??= Environment.GetCommandLineArgs();
            var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < args.Length; index++)
            {
                string current = args[index];
                if (!current.StartsWith("-unityUIFlow.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (index == args.Length - 1)
                {
                    throw new UnityUIFlowException(ErrorCodes.CliArgumentInvalid, $"命令行参数非法：{current}");
                }

                string key = current.Substring(1);
                if (raw.ContainsKey(key))
                {
                    throw new UnityUIFlowException(ErrorCodes.CliArgumentInvalid, $"命令行参数非法：重复参数 {key}");
                }

                raw[key] = args[index + 1];
                index++;
            }

            string configFile = raw.TryGetValue("unityUIFlow.configFile", out string explicitConfig)
                ? explicitConfig
                : Path.Combine(Directory.GetCurrentDirectory(), ".unityuiflow.json");

            Dictionary<string, object> config = _configFileLoader.Load(configFile);
            var options = new CliOptions
            {
                Headed = ReadBool(raw, config, "unityUIFlow.headed", "headed", true),
                ReportPath = ReadString(raw, config, "unityUIFlow.reportPath", "reportPath", "Reports"),
                ScreenshotOnFailure = ReadBool(raw, config, "unityUIFlow.screenshotOnFailure", "screenshotOnFailure", true),
                ScreenshotPath = ReadString(raw, config, "unityUIFlow.screenshotPath", "screenshotPath", null),
                TestFilter = ReadString(raw, config, "unityUIFlow.testFilter", "testFilter", null),
                StopOnFirstFailure = ReadBool(raw, config, "unityUIFlow.stopOnFirstFailure", "stopOnFirstFailure", false),
                ContinueOnStepFailure = ReadBool(raw, config, "unityUIFlow.continueOnStepFailure", "continueOnStepFailure", false),
                DefaultTimeoutMs = ReadInt(raw, config, "unityUIFlow.defaultTimeoutMs", "defaultTimeoutMs", 3000),
                EnableVerboseLog = ReadBool(raw, config, "unityUIFlow.verbose", "verbose", false),
                Batchmode = HasFlag(args, "-batchmode"),
                Nographics = HasFlag(args, "-nographics"),
                ConfigFile = configFile,
                ParsedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            };

            if (options.Batchmode)
            {
                options.Headed = false;
            }

            if (string.IsNullOrWhiteSpace(options.ScreenshotPath))
            {
                options.ScreenshotPath = Path.Combine(options.ReportPath, "Screenshots");
            }

            if (!string.IsNullOrWhiteSpace(options.TestFilter) && options.TestFilter.Length > 256)
            {
                throw new UnityUIFlowException(ErrorCodes.CliFilterInvalid, "测试过滤表达式过长");
            }

            if (options.DefaultTimeoutMs < 100 || options.DefaultTimeoutMs > 600000)
            {
                throw new UnityUIFlowException(ErrorCodes.CliArgumentInvalid, "defaultTimeoutMs 超出允许范围");
            }

            return options;
        }

        /// <summary>
        /// Converts CLI options into runtime options.
        /// </summary>
        public TestOptions ToTestOptions(CliOptions cliOptions)
        {
            var options = new TestOptions
            {
                Headed = cliOptions.Headed,
                ReportOutputPath = cliOptions.ReportPath,
                ScreenshotPath = cliOptions.ScreenshotPath,
                ScreenshotOnFailure = cliOptions.ScreenshotOnFailure,
                StopOnFirstFailure = cliOptions.StopOnFirstFailure,
                ContinueOnStepFailure = cliOptions.ContinueOnStepFailure,
                DefaultTimeoutMs = cliOptions.DefaultTimeoutMs,
                EnableVerboseLog = cliOptions.EnableVerboseLog,
                PreStepDelayMs = cliOptions.PreStepDelayMs,
            };

            if (cliOptions.Batchmode)
            {
                UnityEngine.Debug.Log("[UnityUIFlow] 批处理环境默认关闭 Headed 模式");
            }

            options = UnityUIFlowProjectSettingsUtility.ApplyOverrides(options);
            options.Validate();
            return options;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadString(Dictionary<string, string> cli, Dictionary<string, object> config, string cliKey, string configKey, string defaultValue)
        {
            if (cli.TryGetValue(cliKey, out string cliValue))
            {
                return cliValue;
            }

            if (config.TryGetValue(configKey, out object configValue))
            {
                return configValue?.ToString();
            }

            return defaultValue;
        }

        private static bool ReadBool(Dictionary<string, string> cli, Dictionary<string, object> config, string cliKey, string configKey, bool defaultValue)
        {
            if (cli.TryGetValue(cliKey, out string cliValue))
            {
                return ParseBool(cliKey, cliValue);
            }

            if (config.TryGetValue(configKey, out object configValue))
            {
                return ParseBool(configKey, configValue?.ToString());
            }

            return defaultValue;
        }

        private static int ReadInt(Dictionary<string, string> cli, Dictionary<string, object> config, string cliKey, string configKey, int defaultValue)
        {
            if (cli.TryGetValue(cliKey, out string cliValue))
            {
                return ParseInt(cliKey, cliValue);
            }

            if (config.TryGetValue(configKey, out object configValue))
            {
                return ParseInt(configKey, configValue?.ToString());
            }

            return defaultValue;
        }

        private static bool ParseBool(string name, string value)
        {
            if (!bool.TryParse(value, out bool parsed))
            {
                throw new UnityUIFlowException(ErrorCodes.CliArgumentInvalid, $"参数 {name} 的布尔值非法：{value}");
            }

            return parsed;
        }

        private static int ParseInt(string name, string value)
        {
            if (!int.TryParse(value, out int parsed))
            {
                throw new UnityUIFlowException(ErrorCodes.CliArgumentInvalid, $"参数 {name} 的数值非法：{value}");
            }

            return parsed;
        }
    }

    /// <summary>
    /// Applies wildcard-based YAML filtering.
    /// </summary>
    public static class YamlTestCaseFilter
    {
        /// <summary>
        /// Returns true when the file or case name matches the filter.
        /// </summary>
        public static bool Match(string filter, string yamlPath, string caseName = null)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                return true;
            }

            string regexPattern = "^" + Regex.Escape(filter).Replace("\\*", ".*") + "$";
            string fileName = Path.GetFileNameWithoutExtension(yamlPath) ?? string.Empty;
            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase)
                || (!string.IsNullOrWhiteSpace(caseName) && Regex.IsMatch(caseName, regexPattern, RegexOptions.IgnoreCase));
        }
    }

    /// <summary>
    /// Resolves suite exit codes.
    /// </summary>
    public static class ExitCodeResolver
    {
        /// <summary>
        /// Computes the exit code from a suite result.
        /// </summary>
        public static int Resolve(TestSuiteResult result)
        {
            if (result.Errors > 0)
            {
                return 2;
            }

            if (result.Failed > 0)
            {
                return 1;
            }

            return 0;
        }
    }

    /// <summary>
    /// Writes CI artifact manifests.
    /// </summary>
    public sealed class CiArtifactManifestWriter
    {
        private readonly JsonResultWriter _jsonWriter = new JsonResultWriter();

        /// <summary>
        /// Writes a manifest of report artifacts.
        /// </summary>
        public void Write(string reportRootPath)
        {
            string fullRoot = Path.GetFullPath(reportRootPath);
            if (!Directory.Exists(fullRoot))
            {
                throw new UnityUIFlowException(ErrorCodes.CliReportPathInvalid, $"报告目录不可写：{reportRootPath}");
            }

            var paths = new List<string>();
            foreach (string file in Directory.GetFiles(fullRoot, "*.*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file);
                if (extension != ".md" && extension != ".json" && extension != ".png")
                {
                    continue;
                }

                paths.Add(UnityUIFlowUtility.EnsureRelativeTo(fullRoot, file));
            }

            _jsonWriter.WriteArtifactManifest(paths, Path.Combine(fullRoot, "artifacts.json"));
        }
    }

    /// <summary>
    /// Unity executeMethod entry point for batch runs.
    /// </summary>
    public static class UnityUIFlowCliEntry
    {
        /// <summary>
        /// Runs all YAML files under Assets using command-line options.
        /// </summary>
        public static async void RunAllFromCommandLine()
        {
            int exitCode = 2;
            try
            {
                var parser = new CommandLineOptionsParser();
                CliOptions cliOptions = parser.Parse();
                TestOptions testOptions = parser.ToTestOptions(cliOptions);
                var runner = new TestRunner();
                TestSuiteResult result = await runner.RunSuiteAsync(
                    "Assets",
                    testOptions,
                    (path, caseName) => YamlTestCaseFilter.Match(cliOptions.TestFilter, path, caseName));
                new CiArtifactManifestWriter().Write(testOptions.ReportOutputPath);
                exitCode = result.ExitCode;
            }
            catch (UnityUIFlowException ex)
            {
                exitCode = ex.ErrorCode == ErrorCodes.CliTestsFailed ? 1 : 2;
                UnityEngine.Debug.LogError($"[UnityUIFlow] {ex.ErrorCode}: {ex.Message}");
            }
            catch (Exception ex)
            {
                exitCode = 2;
                UnityEngine.Debug.LogException(ex);
            }
            finally
            {
                UnityEditor.EditorApplication.Exit(exitCode);
            }
        }
    }
}
