#!/usr/bin/env python3
import json
import re
import urllib.request
import time

url = "http://127.0.0.1:8011/mcp"


def call_tool(name, args=None):
    req = urllib.request.Request(url, method="POST")
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json, text/event-stream")
    body = json.dumps(
        {
            "jsonrpc": "2.0",
            "id": 1,
            "method": "tools/call",
            "params": {"name": name, "arguments": args or {}},
        }
    ).encode()
    resp = urllib.request.urlopen(req, body, timeout=30)
    data = resp.read().decode()
    events = re.findall(r"data: (.+)", data)
    if events:
        return json.loads(events[0])
    return {}


def extract_text(result):
    try:
        for c in result["result"]["content"]:
            if c.get("type") == "text":
                return c["text"]
    except Exception:
        pass
    return ""


print("=== unity_sync_after_disk_write (triggerCompile=false) ===")
r = call_tool("unity_sync_after_disk_write", {"triggerCompile": False, "delayS": 2})
t = extract_text(r)
print(t)

# Wait for potential auto-compile
for i in range(10):
    time.sleep(2)
    print(f"\n=== unity_mcp_status (attempt {i+1}) ===")
    r = call_tool("unity_mcp_status")
    t = extract_text(r)
    data = json.loads(t)
    compile_data = data.get("data", {}).get("compile", {})
    print(
        f"status={compile_data.get('status')} errorCount={compile_data.get('errorCount')} warningCount={compile_data.get('warningCount')}"
    )
    if (
        compile_data.get("status") == "finished"
        and compile_data.get("errorCount", 0) > 0
    ):
        print("Auto-compile error detected!")
        break
    if (
        compile_data.get("status") == "idle"
        and compile_data.get("errorCount", 0) > 0
    ):
        print("Error persisted from previous compile!")
        break

print("\n=== unity_compile_errors ===")
r = call_tool("unity_compile_errors")
t = extract_text(r)
print(t)
