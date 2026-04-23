// -----------------------------------------------------------------------
// UnityPilot Editor — https://github.com/codingriver/unitypilot
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace codingriver.unity.pilot
{
    [Serializable]
    internal class BridgeEnvelope
    {
        public string id;
        public string type;
        public string name;
        public string sessionId;
        public string protocolVersion = "1.0";
        public long timestamp;
    }

    [Serializable]
    internal class HelloPayload
    {
        public string unityVersion;
        public string projectPath;
        public string platform;
    }

    [Serializable]
    internal class HeartbeatPayload { }

    /// <summary>session.hello 成功时服务端 payload（含 MCP 显示名与监听地址）。</summary>
    [Serializable]
    internal class HelloAckPayload
    {
        public bool accepted;
        public int heartbeatIntervalMs;
        public string mcpLabel;
        public string mcpHost;
        public int mcpPort;
        /// <summary>MCP Python 进程当前工作目录绝对路径（通常为 Cursor 打开的仓库根目录）。</summary>
        public string mcpWorkingDirectory;
    }

    [Serializable]
    internal class HelloAckMessage
    {
        public string id;
        public string type;
        public string name;
        public HelloAckPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class HelloMessage
    {
        public string id;
        public string type;
        public string name;
        public HelloPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion = "1.0";
    }

    [Serializable]
    internal class HeartbeatMessage
    {
        public string id;
        public string type;
        public string name;
        public HeartbeatPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion = "1.0";
    }

    [Serializable]
    internal class ResultMessage<TPayload>
    {
        public string id;
        public string type = "result";
        public string name;
        public TPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion = "1.0";
    }

    [Serializable]
    internal class EventMessage<TPayload>
    {
        public string id;
        public string type = "event";
        public string name;
        public TPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion = "1.0";
    }

    [Serializable]
    internal class ErrorDetailPayload
    {
        public string commandId;
        public string commandName;
    }

    [Serializable]
    internal class ErrorPayload
    {
        public string code;
        public string message;
        public ErrorDetailPayload detail;
    }

    [Serializable]
    internal class ErrorMessage
    {
        public string id;
        public string type = "error";
        public string name;
        public ErrorPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion = "1.0";
    }

    [Serializable]
    internal class DomainReloadPayload
    {
        public string phase; // "starting" or "completed"
        public bool isCompiling;
        public string playModeState;
    }

    [Serializable]
    internal class CompileRequestMessage
    {
        public string id;
        public string type;
        public string name;
        public CompileRequestPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class CompileRequestPayload
    {
        public string requestId;
    }

    [Serializable]
    internal class CompileAcceptedPayload
    {
        public bool accepted;
        public string compileRequestId;
    }

    [Serializable]
    internal class CompileStatusPayload
    {
        public string requestId;
        public string status;
        public int errorCount;
        public int warningCount;
        public long startedAt;
        public long finishedAt;
    }

    [Serializable]
    internal class CompileErrorItemPayload
    {
        public string file;
        public int line;
        public int column;
        public string message;
        public string severity;
    }

    [Serializable]
    internal class CompileErrorsPayload
    {
        public string requestId;
        public int total;
        public List<CompileErrorItemPayload> errors = new();
    }

    /// <summary>MCP-initiated compile lifecycle (explicit compile.started / compile.finished events).</summary>
    [Serializable]
    internal class CompileLifecyclePayload
    {
        public string phase;
        public string requestId;
        public string source;
        public long startedAt;
        public long finishedAt;
        public int errorCount;
        public int warningCount;
        public long durationMs;
    }

    /// <summary>Any script compilation via CompilationPipeline (editor UI or MCP).</summary>
    [Serializable]
    internal class CompilePipelinePayload
    {
        public string phase;
        public string source;
        public long startedAt;
        public long durationMs;
    }

    [Serializable]
    internal class CompileWaitMessage
    {
        public string id;
        public string type;
        public string name;
        public CompileWaitPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class CompileWaitPayload
    {
        public int timeoutMs = 300000;
    }

    [Serializable]
    internal class EditorDelayMessage
    {
        public string id;
        public string type;
        public string name;
        public EditorDelayPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class EditorDelayPayload
    {
        public int delayMs;
    }

    [Serializable]
    internal class EditorWindowCloseMessage
    {
        public string id;
        public string type;
        public string name;
        public EditorWindowClosePayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class EditorWindowClosePayload
    {
        public string windowTitle;
        public string matchMode;
    }

    [Serializable]
    internal class EditorWindowCloseResultPayload
    {
        public bool ok;
        public string state;
        public string deniedReason;
        public string matchedTitle;
        public string matchedTypeName;
        public bool multipleMatches;
    }

    [Serializable]
    internal class EditorWindowSetRectMessage
    {
        public string id;
        public string type;
        public string name;
        public EditorWindowSetRectPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class EditorWindowSetRectPayload
    {
        public string windowTitle;
        public string matchMode;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [Serializable]
    internal class UIToolkitScrollbarDragMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitScrollbarDragPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitScrollbarDragPayload
    {
        public string targetWindow;
        public string scrollViewElementName;
        /// <summary>Optional nested path; when set, resolves outer→inner ScrollViews (same format as uitoolkit.scroll).</summary>
        public string scrollViewNamePath;
        public string scrollbarAxis;
        public float normalizedThumbPosition;
        public int dragSteps;
    }

    [Serializable]
    internal class UIToolkitScrollbarDragResultPayload
    {
        public bool ok;
        public string state;
        public float scrollOffsetX;
        public float scrollOffsetY;
    }

    [Serializable]
    internal class PlayModeSetMessage
    {
        public string id;
        public string type;
        public string name;
        public PlayModeSetPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class PlayModeSetPayload
    {
        public string action;
    }

    [Serializable]
    internal class PlayModeChangedPayload
    {
        public string state;
    }

    [Serializable]
    internal class EditorStatePayload
    {
        public bool connected;
        public bool isCompiling;
        public string playModeState;
        public string activeScene;
    }

    [Serializable]
    internal class MouseEventMessage
    {
        public string id;
        public string type;
        public string name;
        public MouseEventPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class MouseEventPayload
    {
        public string targetWindow;
        public string action;
        public string button;
        public float x;
        public float y;
        public string[] modifiers;
        public float scrollDeltaX;
        public float scrollDeltaY;
        public string elementName;
        public int elementIndex = -1;
    }

    [Serializable]
    internal class GenericOkPayload
    {
        public bool ok;
        public string state;
        public string status;
    }

    [Serializable]
    internal class GenericOkEnvelope
    {
        public string id;
        public string type;
        public string name;
        public GenericOkPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class KeyboardEventMessage
    {
        public string id;
        public string type;
        public string name;
        public KeyboardEventPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class KeyboardEventPayload
    {
        public string targetWindow;
        public string action;    // keydown, keyup, keypress, type
        public string keyCode;   // Unity KeyCode name (e.g. "A", "Return", "Space")
        public char character;   // single character (for keydown with specific char)
        public string text;      // text to type (for "type" action)
        public string[] modifiers; // shift, ctrl/control, alt, cmd/command
    }

    [Serializable]
    internal class UIToolkitDumpMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitDumpPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitDumpPayload
    {
        public string targetWindow;
        public int maxDepth = 10;
    }

    [Serializable]
    internal class UIToolkitQueryMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitQueryPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitQueryPayload
    {
        public string targetWindow;
        public string nameFilter;
        public string classFilter;
        public string typeFilter;
        public string textFilter;
    }

    [Serializable]
    internal class UIToolkitEventMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitEventPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitEventPayload
    {
        public string targetWindow;
        public string eventType;
        public string elementName;
        public int elementIndex = -1;
        public string keyCode;
        public string character;
        public int mouseButton;
        public float mouseX;
        public float mouseY;
        public float wheelDeltaX;
        public float wheelDeltaY;
        public string[] modifiers;
    }

    [Serializable]
    internal class UIToolkitElementInfo
    {
        public int index;
        public int parentIndex;
        public int depth;
        public string typeName;
        public string name;
        public string classes;
        public float worldBoundX;
        public float worldBoundY;
        public float worldBoundWidth;
        public float worldBoundHeight;
        public float localBoundX;
        public float localBoundY;
        public bool visible;
        public bool enabled;
        public int childCount;
        public string text;
        public string value;
        public string valueType;
        public bool interactable;
        public bool isFocused;
    }

    [Serializable]
    internal class UIToolkitSetValueMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitSetValuePayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitSetValuePayload
    {
        public string targetWindow;
        public string elementName;
        public int elementIndex = -1;
        public string value;
    }

    [Serializable]
    internal class UIToolkitInteractMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitInteractPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitInteractPayload
    {
        public string targetWindow;
        public string elementName;
        public int elementIndex = -1;
        public string action; // "click", "focus", "blur"
    }

    [Serializable]
    internal class UIToolkitDumpResultPayload
    {
        public bool ok;
        public string targetWindow;
        public int totalElements;
        public List<UIToolkitElementInfo> elements = new();
    }

    [Serializable]
    internal class UIToolkitQueryResultPayload
    {
        public bool ok;
        public int matchCount;
        public List<UIToolkitElementInfo> matches = new();
    }

    [Serializable]
    internal class UIToolkitScrollMessage
    {
        public string id;
        public string type;
        public string name;
        public UIToolkitScrollPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class UIToolkitScrollPayload
    {
        public string targetWindow;
        public string elementName;
        public int elementIndex = -1;
        /// <summary>Nested ScrollView names from outer to inner, separated by | or / (M27 BL-07).</summary>
        public string scrollViewNamePath;
        public float scrollToX = -1;
        public float scrollToY = -1;
        public float deltaX;
        public float deltaY;
        public string mode = "absolute"; // "absolute" or "delta"
    }

    [Serializable]
    internal class UIToolkitScrollResultPayload
    {
        public bool ok;
        public string state;
        public float scrollOffsetX;
        public float scrollOffsetY;
    }

    [Serializable]
    internal class DragDropMessage
    {
        public string id;
        public string type;
        public string name;
        public DragDropPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class DragDropPayload
    {
        public string sourceWindow;
        public string targetWindow;
        public string dragType;       // "asset", "gameobject", "custom"
        public float fromX;
        public float fromY;
        public float toX;
        public float toY;
        public string[] assetPaths;   // for dragType="asset"
        public ulong[] gameObjectIds;   // for dragType="gameobject" (EntityId wire ulong)
        public string customData;     // for dragType="custom"
        public string[] modifiers;
    }

    [Serializable]
    internal class DragDropResultPayload
    {
        public bool ok;
        public string state;
        public string dragType;
        public string visualMode;     // DragAndDrop.visualMode.ToString()
    }

    // ── M25 RShell（UDP 在 Bridge 内实现；Python 仅转发）────────────────────────

    [Serializable]
    internal class RShellConnectPayload
    {
        public string host;
        public int port = 9999;
        public int timeoutMs = 10000;
        public int maxRetries = 3;
    }

    [Serializable]
    internal class RShellConnectMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellConnectPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellConnectResultPayload
    {
        public bool ok;
        public string connectionId;
        public string status;
        public string error;
        public string host;
        public int port;
    }

    [Serializable]
    internal class RShellConnectionIdPayload
    {
        public string connectionId;
    }

    [Serializable]
    internal class RShellConnectionIdMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellConnectionIdPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellExecutePayload
    {
        public string connectionId;
        public string expression;
    }

    [Serializable]
    internal class RShellExecuteMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellExecutePayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellExecuteResultPayload
    {
        public bool ok;
        public string status;
        public string result;
        public string error;
        public long roundTripMs;
    }

    [Serializable]
    internal class RShellSceneInfoPayload
    {
        public string connectionId;
        public string path;
    }

    [Serializable]
    internal class RShellSceneInfoMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellSceneInfoPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellGetValuePayload
    {
        public string connectionId;
        public string expression;
    }

    [Serializable]
    internal class RShellGetValueMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellGetValuePayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellSetValuePayload
    {
        public string connectionId;
        public string expression;
        public string value;
    }

    [Serializable]
    internal class RShellSetValueMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellSetValuePayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellCallMethodPayload
    {
        public string connectionId;
        public string expression;
        public string args;
    }

    [Serializable]
    internal class RShellCallMethodMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellCallMethodPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellStatusPayload
    {
        public string connectionId;
    }

    [Serializable]
    internal class RShellStatusMessage
    {
        public string id;
        public string type;
        public string name;
        public RShellStatusPayload payload;
        public long timestamp;
        public string sessionId;
        public string protocolVersion;
    }

    [Serializable]
    internal class RShellStatusResultPayload
    {
        public bool ok;
        public string connectionId;
        public string status;
        public string host;
        public int port;
        public string error;
        public long lastHeartbeatAt;
        public RShellStatusEntry[] connections;
    }

    [Serializable]
    internal class RShellStatusEntry
    {
        public string connectionId;
        public string status;
        public string host;
        public int port;
        public long lastHeartbeatAt;
        public string error;
    }
}
