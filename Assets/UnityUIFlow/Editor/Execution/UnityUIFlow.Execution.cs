using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIFlow
{
    /// <summary>
    /// Shared execution context for a single test run.
    /// </summary>
    public sealed class ExecutionContext : IDisposable
    {
        public VisualElement Root;
        public EditorWindow ManagedWindow;
        public ElementFinder Finder;
        public TestOptions Options;
        public MarkdownReporter Reporter;
        public ScreenshotManager ScreenshotManager;
        public ActionRegistry ActionRegistry;
        public RuntimeController RuntimeController;
        internal UnityUIFlowSimulationSession SimulationSession;
        public Dictionary<string, object> SharedBag = new Dictionary<string, object>(StringComparer.Ordinal);
        public CancellationToken CancellationToken;
        public string CaseName;

        public void Dispose()
        {
            try
            {
                SimulationSession?.Dispose();
                SimulationSession = null;
            }
            finally
            {
                if (ManagedWindow != null)
                {
                    ManagedWindow.Close();
                    ManagedWindow = null;
                }

                RuntimeController?.Dispose();
            }
        }
    }

    /// <summary>
    /// Controls pause, resume, step and stop state.
    /// </summary>
    public sealed class RuntimeController : IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isPaused;
        private bool _stepRequested;
        private bool _isStopped;
        private bool _pausedForFailure;

        public HeadedRunMode RunMode { get; set; } = HeadedRunMode.Continuous;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public bool IsPaused => _isPaused;

        public bool IsStopped => _isStopped;

        public bool IsPausedForFailure => _pausedForFailure;

        public void Pause()
        {
            _isPaused = true;
        }

        public void PauseForFailure()
        {
            _isPaused = true;
            _pausedForFailure = true;
        }

        public void Resume()
        {
            _isPaused = false;
            _stepRequested = false;
            _pausedForFailure = false;
        }

        public void StepOnce()
        {
            _isPaused = false;
            _stepRequested = true;
            _pausedForFailure = false;
        }

        public void Stop()
        {
            _isStopped = true;
            _cancellationTokenSource?.Cancel();
        }

        public async Task WaitIfPausedAsync()
        {
            while (_isPaused && !_isStopped)
            {
                await EditorAsyncUtility.NextFrameAsync(CancellationToken);
            }
        }

        public void OnStepCompleted()
        {
            if (RunMode == HeadedRunMode.Step)
            {
                if (_stepRequested)
                {
                    _stepRequested = false;
                }

                _isPaused = true;
            }
        }

        public void Dispose()
        {
            if (_cancellationTokenSource == null)
            {
                return;
            }

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    internal static class TestHostWindowManager
    {
        public static async Task<(EditorWindow window, VisualElement root)> OpenAsync(HostWindowDefinition hostWindow)
        {
            if (hostWindow == null || string.IsNullOrWhiteSpace(hostWindow.Type))
            {
                throw new UnityUIFlowException(ErrorCodes.HostWindowTypeInvalid, "Host window type is required.");
            }

            Type resolvedType = ResolveType(hostWindow.Type);
            if (hostWindow.ReopenIfOpen)
            {
                CloseOpenInstances(resolvedType);
                await EditorAsyncUtility.NextFrameAsync(CancellationToken.None);
            }

            EditorWindow window;
            try
            {
                window = EditorWindow.GetWindow(resolvedType);
                window.Show();
                window.Focus();
            }
            catch (Exception ex)
            {
                throw new UnityUIFlowException(ErrorCodes.HostWindowOpenFailed, $"Failed to open host window {hostWindow.Type}: {ex.Message}", ex);
            }

            await EditorAsyncUtility.NextFrameAsync(CancellationToken.None);
            if (window is IUnityUIFlowTestHostWindow preparedWindow)
            {
                preparedWindow.PrepareForAutomatedTest();
                await EditorAsyncUtility.NextFrameAsync(CancellationToken.None);
            }

            if (window.rootVisualElement == null)
            {
                window.Close();
                throw new UnityUIFlowException(ErrorCodes.HostWindowOpenFailed, $"Host window root is missing: {hostWindow.Type}");
            }

            return (window, window.rootVisualElement);
        }

        private static Type ResolveType(string typeName)
        {
            var matches = TypeCache.GetTypesDerivedFrom<EditorWindow>()
                .Where(type => string.Equals(type.FullName, typeName, StringComparison.Ordinal) || string.Equals(type.Name, typeName, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            if (matches.Count > 1)
            {
                Type fullNameMatch = matches.FirstOrDefault(type => string.Equals(type.FullName, typeName, StringComparison.Ordinal));
                if (fullNameMatch != null)
                {
                    return fullNameMatch;
                }

                throw new UnityUIFlowException(ErrorCodes.HostWindowTypeInvalid, $"Host window type is ambiguous: {typeName}");
            }

            throw new UnityUIFlowException(ErrorCodes.HostWindowTypeInvalid, $"Host window type was not found: {typeName}");
        }

        private static void CloseOpenInstances(Type type)
        {
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null && window.GetType() == type)
                {
                    window.Close();
                }
            }
        }
    }

    /// <summary>
    /// Executes a single compiled step.
    /// </summary>
    public sealed class StepExecutor
    {
        /// <summary>
        /// Executes one step and returns a step result.
        /// </summary>
        public async Task<StepResult> ExecuteStepAsync(ExecutableStep step, ExecutionContext context, int stepIndex)
        {
            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            var result = new StepResult
            {
                StepId = step.StepId,
                DisplayName = step.DisplayName,
                StartedAtUtc = startedAt.ToString("O"),
            };

            bool verboseLog = context.Options?.EnableVerboseLog == true;

            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                VisualElement highlightedElement = null;
                ActionContext actionContext = null;

                if (step.Condition != null && !context.Finder.Exists(step.Condition.SelectorExpression, context.Root, true))
                {
                    if (verboseLog)
                        Debug.Log($"[UnityUIFlow][{context.CaseName}] 步骤[{stepIndex}] \"{step.DisplayName}\" 条件不满足，跳过");
                    result.Status = TestStatus.Skipped;
                    result.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                    result.DurationMs = UnityUIFlowUtility.DurationMs(startedAt, DateTimeOffset.UtcNow);
                    HeadedRunEventBus.PublishStepCompleted(step, result, null);
                    return result;
                }

                if (verboseLog)
                    Debug.Log($"[UnityUIFlow][{context.CaseName}] 步骤[{stepIndex}] 开始 \"{step.DisplayName}\" 动作={step.ActionName} 超时={step.TimeoutMs}ms");

                HeadedRunEventBus.PublishStepStarted(step);
                if (step.Selector != null)
                {
                    highlightedElement = context.Finder.Find(step.Selector, context.Root, false).Element;
                    if (highlightedElement != null)
                    {
                        if (verboseLog)
                            Debug.Log($"[UnityUIFlow][{context.CaseName}] 步骤[{stepIndex}] 预查找元素 {step.Selector.Raw} => {ActionContext.ElementInfo(highlightedElement)}");
                        HeadedRunEventBus.PublishHighlightedElement(step, highlightedElement);
                    }
                }

                if (context.Options?.PreStepDelayMs > 0)
                {
                    if (verboseLog)
                    {
                        Debug.Log($"[UnityUIFlow][{context.CaseName}] 步骤[{stepIndex}] 调试延迟 {context.Options.PreStepDelayMs}ms");
                    }

                    await EditorAsyncUtility.DelayAsync(context.Options.PreStepDelayMs, context.CancellationToken);
                }

                var timeoutController = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                try
                {
                    timeoutController.CancelAfter(step.TimeoutMs);
                    actionContext = new ActionContext
                    {
                        Root = context.Root,
                        Finder = context.Finder,
                        Options = context.Options,
                        Reporter = context.Reporter,
                        CurrentStepId = step.StepId,
                        CurrentCaseName = context.CaseName,
                        CurrentStepIndex = stepIndex,
                        SharedBag = context.SharedBag,
                        CancellationToken = timeoutController.Token,
                        ScreenshotManager = context.ScreenshotManager,
                        RuntimeController = context.RuntimeController,
                        Simulator = context.SimulationSession?.PointerDriver,
                        SimulationSession = context.SimulationSession,
                    };
                    actionContext.SharedBag.Remove("inputDriver.host");
                    actionContext.SharedBag.Remove("inputDriver.pointer");
                    actionContext.SharedBag.Remove("inputDriver.keyboard");
                    actionContext.SharedBag.Remove("officialUiToolkit.describe");
                    actionContext.SharedBag.Remove("driver.binding.summary");
                    actionContext.SharedBag["inputDriver.host"] = context.SimulationSession?.HostDriverName ?? "RootOverrideOnly";
                    actionContext.SharedBag["officialUiToolkit.describe"] = context.SimulationSession?.OfficialUiToolkit.Describe() ?? "unavailable";
                    actionContext.SharedBag["driver.binding.summary"] = context.SimulationSession?.DescribeDrivers() ?? "host=RootOverrideOnly; pointer=UIToolkitFallbackOnly; keyboard=UIToolkitFallbackOnly; official=unavailable";

                    if (step.Kind == ExecutableStepKind.Loop)
                    {
                        if (verboseLog)
                            Debug.Log($"[UnityUIFlow][{context.CaseName}] 步骤[{stepIndex}] 进入循环，最大迭代 {step.Loop.MaxIterations}");
                        await ExecuteLoopAsync(step, context, stepIndex, timeoutController.Token);
                    }
                    else
                    {
                        IAction action = context.ActionRegistry.Resolve(step.ActionName);
                        await action.ExecuteAsync(context.Root, actionContext, step.Parameters);
                    }

                    if (actionContext.CurrentAttachments.Count > 0)
                    {
                        result.Attachments.AddRange(actionContext.CurrentAttachments);
                        result.ScreenshotPath = actionContext.CurrentAttachments.FirstOrDefault();
                        if (actionContext.ScreenshotManager != null
                            && !string.IsNullOrWhiteSpace(result.ScreenshotPath))
                        {
                            result.ScreenshotSource = actionContext.ScreenshotManager.LastCaptureSource;
                        }
                    }

                    if (actionContext.SharedBag.TryGetValue("inputDriver.host", out object hostDriver))
                    {
                        result.HostDriver = hostDriver?.ToString();
                    }

                    if (actionContext.SharedBag.TryGetValue("inputDriver.pointer", out object pointerDriver))
                    {
                        result.PointerDriver = pointerDriver?.ToString();
                    }

                    if (actionContext.SharedBag.TryGetValue("inputDriver.keyboard", out object keyboardDriver))
                    {
                        result.KeyboardDriver = keyboardDriver?.ToString();
                    }

                    if (actionContext.SharedBag.TryGetValue("driver.binding.summary", out object driverDetails)
                        || actionContext.SharedBag.TryGetValue("officialUiToolkit.describe", out driverDetails))
                    {
                        result.DriverDetails = driverDetails?.ToString();
                    }
                }
                catch (OperationCanceledException) when (!context.CancellationToken.IsCancellationRequested)
                {
                    throw new UnityUIFlowException(ErrorCodes.StepTimeout, $"步骤 {step.DisplayName} 执行超时：{step.TimeoutMs}ms");
                }
                finally
                {
                    timeoutController.Dispose();
                }

                result.Status = TestStatus.Passed;
            }
            catch (OperationCanceledException)
            {
                result.Status = TestStatus.Error;
                result.ErrorCode = ErrorCodes.TestRunAborted;
                result.ErrorMessage = "测试运行已停止";
            }
            catch (UnityUIFlowException ex)
            {
                result.Status = ex.ErrorCode == ErrorCodes.ActionExecutionFailed || ex.ErrorCode == ErrorCodes.StepTimeout
                    ? TestStatus.Failed
                    : TestStatus.Error;
                result.ErrorCode = ex.ErrorCode;
                result.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                result.Status = TestStatus.Error;
                result.ErrorCode = ErrorCodes.StepExecutionException;
                result.ErrorMessage = ex.Message;
            }

            if ((result.Status == TestStatus.Failed || result.Status == TestStatus.Error) && context.Options.ScreenshotOnFailure)
            {
                try
                {
                    string screenshotPath = await context.ScreenshotManager.CaptureAsync(context.CaseName, stepIndex, "failure", context.CancellationToken);
                    result.ScreenshotPath = screenshotPath;
                    result.ScreenshotSource = context.ScreenshotManager.LastCaptureSource;
                    result.Attachments.Add(screenshotPath);
                }
                catch (Exception captureException)
                {
                    result.ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? captureException.Message
                        : result.ErrorMessage + Environment.NewLine + captureException.Message;
                }
            }

            if ((result.Status == TestStatus.Failed || result.Status == TestStatus.Error)
                && context.Options.Headed
                && context.Options.DebugOnFailure)
            {
                context.RuntimeController?.PauseForFailure();
            }

            result.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
            result.DurationMs = UnityUIFlowUtility.DurationMs(startedAt, DateTimeOffset.UtcNow);

            if (verboseLog)
            {
                string statusMark = result.Status == TestStatus.Passed ? "✓" : result.Status == TestStatus.Skipped ? "⊘" : "✗";
                string errorDetail = string.IsNullOrWhiteSpace(result.ErrorMessage) ? string.Empty : $" | {result.ErrorCode}: {result.ErrorMessage}";
                Debug.Log($"[UnityUIFlow][{context.CaseName}] 步骤[{stepIndex}] {statusMark} \"{step.DisplayName}\" {result.DurationMs}ms{errorDetail}");
            }

            VisualElement completedElement = step.Selector != null ? context.Finder.Find(step.Selector, context.Root, false).Element : null;
            HeadedRunEventBus.PublishStepCompleted(step, result, completedElement);
            return result;
        }

        private static async Task ExecuteLoopAsync(ExecutableStep step, ExecutionContext context, int stepIndex, CancellationToken cancellationToken)
        {
            int iterations = 0;
            while (context.Finder.Exists(step.Loop.Condition.SelectorExpression, context.Root, true))
            {
                cancellationToken.ThrowIfCancellationRequested();
                iterations++;
                if (iterations > step.Loop.MaxIterations)
                {
                    throw new UnityUIFlowException(ErrorCodes.TestLoopLimitExceeded, $"步骤 {step.DisplayName} 超过最大循环次数 {step.Loop.MaxIterations}");
                }

                foreach (ExecutableStep loopStep in step.Loop.Steps)
                {
                    StepResult nested = await new StepExecutor().ExecuteStepAsync(loopStep, context, stepIndex);
                    if (nested.Status == TestStatus.Failed || nested.Status == TestStatus.Error)
                    {
                        throw new UnityUIFlowException(nested.ErrorCode ?? ErrorCodes.ActionExecutionFailed, nested.ErrorMessage ?? $"循环步骤 {loopStep.DisplayName} 执行失败");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Main test runner entry point.
    /// </summary>
    public sealed class TestRunner
    {
        private readonly YamlTestCaseParser _parser = new YamlTestCaseParser();
        private readonly SelectorCompiler _selectorCompiler = new SelectorCompiler();
        private readonly ActionRegistry _actionRegistry = new ActionRegistry();
        private readonly StepExecutor _stepExecutor = new StepExecutor();

        /// <summary>
        /// Runs a single YAML test file.
        /// </summary>
        public Task<TestResult> RunFileAsync(string yamlPath, TestOptions options = null, VisualElement rootOverride = null, Action<ExecutionContext> onContextReady = null)
        {
            if (string.IsNullOrWhiteSpace(yamlPath))
            {
                throw new UnityUIFlowException(ErrorCodes.TestCasePathInvalid, "测试用例路径非法");
            }

            TestCaseDefinition definition = _parser.ParseFile(yamlPath);
            return RunDefinitionAsync(definition, options ?? new TestOptions(), rootOverride, onContextReady);
        }
        public Task<TestResult> RunAsync(string yamlContent, TestOptions options = null, VisualElement rootOverride = null)
        {
            TestCaseDefinition definition = _parser.Parse(yamlContent, "inline.yaml");
            return RunDefinitionAsync(definition, options ?? new TestOptions(), rootOverride);
        }
        /// <summary>
        /// Runs YAML content against a specific root.
        /// </summary>
        public Task<TestResult> RunAsync(string yamlContent, string sourcePath, VisualElement root, TestOptions options = null, Action<ExecutionContext> onContextReady = null)
        {
            TestCaseDefinition definition = _parser.Parse(yamlContent, sourcePath ?? "inline.yaml");
            return RunDefinitionAsync(definition, options ?? new TestOptions(), root, onContextReady);
        }

        /// <summary>
        /// Runs all YAML files under a directory.
        /// </summary>
        public async Task<TestSuiteResult> RunSuiteAsync(string directory, TestOptions options = null, Func<string, string, bool> filter = null, VisualElement rootOverride = null)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                throw new UnityUIFlowException(ErrorCodes.TestSuiteDirectoryNotFound, $"测试目录不存在：{directory}");
            }

            options ??= new TestOptions();
            options.Validate();

            string[] yamlFiles = Directory.GetFiles(directory, "*.yaml", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var suiteResult = new TestSuiteResult
            {
                StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            };

            if (yamlFiles.Length == 0)
            {
                suiteResult.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                suiteResult.ExitCode = 0;
                return suiteResult;
            }

            foreach (string yamlFile in yamlFiles)
            {
                TestResult testResult;
                try
                {
                    TestCaseDefinition definition = _parser.ParseFile(yamlFile);
                    if (filter != null && !filter(yamlFile, definition.Name))
                    {
                        continue;
                    }

                    testResult = await RunDefinitionAsync(definition, options, rootOverride);
                }
                catch (Exception ex)
                {
                    testResult = new TestResult
                    {
                        CaseName = Path.GetFileNameWithoutExtension(yamlFile),
                        Status = TestStatus.Error,
                        StartedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                        EndedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
                        ErrorCode = ex is UnityUIFlowException flowException ? flowException.ErrorCode : ErrorCodes.CliExecutionError,
                        ErrorMessage = ex.Message,
                    };
                }

                suiteResult.CaseResults.Add(testResult);
                suiteResult.Total++;
                switch (testResult.Status)
                {
                    case TestStatus.Passed:
                        suiteResult.Passed++;
                        break;
                    case TestStatus.Failed:
                        suiteResult.Failed++;
                        break;
                    case TestStatus.Error:
                        suiteResult.Errors++;
                        break;
                    case TestStatus.Skipped:
                        suiteResult.Skipped++;
                        break;
                }

                if (options.StopOnFirstFailure && (testResult.Status == TestStatus.Failed || testResult.Status == TestStatus.Error))
                {
                    break;
                }
            }

            suiteResult.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
            suiteResult.ExitCode = ExitCodeResolver.Resolve(suiteResult);
            var reporter = new MarkdownReporter(new ReporterOptions
            {
                ReportRootPath = options.ReportOutputPath,
                ScreenshotRootPath = options.ScreenshotPath,
                SuiteName = null,
            });
            try
            {
                reporter.WriteSuiteReport(suiteResult);
            }
            catch (Exception reportException)
            {
                Debug.LogError($"[UnityUIFlow] {ErrorCodes.ReportWriteFailed}: {reportException.Message}");
            }

            return suiteResult;
        }

        private async Task<TestResult> RunDefinitionAsync(TestCaseDefinition definition, TestOptions options, VisualElement rootOverride, Action<ExecutionContext> onContextReady = null)
        {
            options = UnityUIFlowProjectSettingsUtility.ApplyOverrides(options);
            options.Validate();
            if (options.RetryCount.HasValue)
            {
                Debug.LogWarning($"[UnityUIFlow] RetryCount={options.RetryCount.Value} 在 V1 中仅记录 Warning 并忽略");
            }

            VisualElement root = rootOverride;
            EditorWindow managedWindow = null;
            if (root == null)
            {
                (managedWindow, root) = await ResolveExecutionRootAsync(definition);
            }
            if (root == null)
            {
                throw new UnityUIFlowException(ErrorCodes.RootElementMissing, "未找到可执行的根节点");
            }

            var reportOptions = new ReporterOptions
            {
                ReportRootPath = options.ReportOutputPath,
                ScreenshotRootPath = options.ScreenshotPath,
            };

            var simulationSession = new UnityUIFlowSimulationSession();
            if (managedWindow != null)
            {
                simulationSession.BindEditorWindowHost(managedWindow, "HostWindowManager(EditorWindow.GetWindow)");
            }
            else
            {
                simulationSession.BindHostDriver("RootOverrideOnly");
            }
            if (options.RequireOfficialHost && !simulationSession.HasExecutableOfficialHost)
            {
                throw new UnityUIFlowException(
                    ErrorCodes.FixtureWindowCreateFailed,
                    $"正式验收模式下未能创建官方测试宿主：{definition.Fixture?.HostWindow?.Type ?? definition.Name}");
            }

            var context = new ExecutionContext
            {
                Root = root,
                ManagedWindow = managedWindow,
                Finder = new ElementFinder(),
                Options = options,
                Reporter = new MarkdownReporter(reportOptions),
                ScreenshotManager = new ScreenshotManager(options, () => ResolveScreenshotWindow(managedWindow, root)),
                ActionRegistry = _actionRegistry,
                RuntimeController = new RuntimeController(),
                SimulationSession = simulationSession,
                CaseName = definition.Name,
            };
            context.CancellationToken = context.RuntimeController.CancellationToken;
            onContextReady?.Invoke(context);

            if (options.Headed)
            {
                HeadedRunEventBus.PublishRunAttached(context.RuntimeController, definition.Name);
            }

            if (options.EnableVerboseLog)
            {
                Debug.Log($"[UnityUIFlow] 开始执行用例 \"{definition.Name}\"");
            }

            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            var result = new TestResult
            {
                CaseName = definition.Name,
                StartedAtUtc = startedAt.ToString("O"),
            };

            try
            {
                var planBuilder = new ExecutionPlanBuilder(_selectorCompiler, _actionRegistry);
                ExecutionPlan plan = planBuilder.Build(definition, options);
                bool abortMainFlow = false;

                for (int index = 0; index < plan.Steps.Count; index++)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();
                    await context.RuntimeController.WaitIfPausedAsync();

                    ExecutableStep step = plan.Steps[index];
                    if (abortMainFlow && step.Phase != StepPhase.Teardown)
                    {
                        continue;
                    }

                    StepResult stepResult = await _stepExecutor.ExecuteStepAsync(step, context, index + 1);
                    if (!string.IsNullOrWhiteSpace(stepResult.ScreenshotPath))
                    {
                        stepResult.ScreenshotPath = UnityUIFlowUtility.EnsureRelativeTo(options.ReportOutputPath, stepResult.ScreenshotPath);
                    }

                    context.Reporter.RecordStepResult(result.CaseName, stepResult, Array.Empty<string>());
                    result.StepResults.Add(stepResult);

                    if (stepResult.Status == TestStatus.Failed || stepResult.Status == TestStatus.Error)
                    {
                        if (options.Headed)
                        {
                            HeadedRunEventBus.PublishFailure(step, stepResult);
                        }

                        if (!step.ContinueOnFailure)
                        {
                            if (step.Phase == StepPhase.Teardown)
                            {
                                break;
                            }

                            abortMainFlow = true;
                        }
                    }

                    context.RuntimeController.OnStepCompleted();
                }

                if (context.RuntimeController.IsPausedForFailure && !context.RuntimeController.IsStopped)
                {
                    await context.RuntimeController.WaitIfPausedAsync();
                }
            }
            catch (OperationCanceledException)
            {
                result.ErrorCode = ErrorCodes.TestRunAborted;
                result.ErrorMessage = "测试运行已停止";
                result.Status = TestStatus.Error;
            }
            catch (UnityUIFlowException ex)
            {
                result.ErrorCode = ex.ErrorCode;
                result.ErrorMessage = ex.Message;
                result.Status = TestStatus.Error;
            }
            catch (Exception ex)
            {
                result.ErrorCode = ErrorCodes.CliExecutionError;
                result.ErrorMessage = ex.Message;
                result.Status = TestStatus.Error;
            }
            finally
            {
                result.EndedAtUtc = DateTimeOffset.UtcNow.ToString("O");
                result.DurationMs = UnityUIFlowUtility.DurationMs(startedAt, DateTimeOffset.UtcNow);
                if (result.Status != TestStatus.Error)
                {
                    result.Status = ComputeStatus(result.StepResults);
                }

                try
                {
                    context.Reporter.WriteCaseReport(result);
                }
                catch (Exception reportException)
                {
                    Debug.LogError($"[UnityUIFlow] {ErrorCodes.ReportWriteFailed}: {reportException.Message}");
                }

                context.Dispose();
                HeadedRunEventBus.PublishRunFinished(result);

                if (options.EnableVerboseLog)
                {
                    Debug.Log($"[UnityUIFlow] 用例 \"{definition.Name}\" 完成 状态={result.Status} 耗时={result.DurationMs}ms");
                }
            }

            return result;
        }

        private static TestStatus ComputeStatus(List<StepResult> steps)
        {
            if (steps == null || steps.Count == 0)
            {
                return TestStatus.Skipped;
            }

            bool anyPassed = false;
            bool anySkipped = false;
            foreach (StepResult step in steps)
            {
                switch (step.Status)
                {
                    case TestStatus.Error:
                        return TestStatus.Error;
                    case TestStatus.Failed:
                        return TestStatus.Failed;
                    case TestStatus.Passed:
                        anyPassed = true;
                        break;
                    case TestStatus.Skipped:
                        anySkipped = true;
                        break;
                }
            }

            if (anyPassed)
            {
                return TestStatus.Passed;
            }

            return anySkipped ? TestStatus.Skipped : TestStatus.Passed;
        }

        private static VisualElement ResolveDefaultRoot()
        {
            EditorWindow focused = EditorWindow.focusedWindow;
            if (focused != null && !(focused is HeadedTestWindow) && focused.rootVisualElement != null)
            {
                return focused.rootVisualElement;
            }

            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (EditorWindow window in windows)
            {
                if (window == null || window is HeadedTestWindow)
                {
                    continue;
                }

                if (window.rootVisualElement != null)
                {
                    return window.rootVisualElement;
                }
            }

            return null;
        }

        private static async Task<(EditorWindow window, VisualElement root)> ResolveExecutionRootAsync(TestCaseDefinition definition)
        {
            if (definition?.Fixture?.HostWindow != null)
            {
                return await TestHostWindowManager.OpenAsync(definition.Fixture.HostWindow);
            }

            return (null, ResolveDefaultRoot());
        }

        private static EditorWindow ResolveScreenshotWindow(EditorWindow managedWindow, VisualElement root)
        {
            if (managedWindow != null)
            {
                return managedWindow;
            }

            if (root?.panel == null)
            {
                return EditorWindow.focusedWindow;
            }

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null && window.rootVisualElement?.panel == root.panel)
                {
                    return window;
                }
            }

            return EditorWindow.focusedWindow;
        }
    }
}
