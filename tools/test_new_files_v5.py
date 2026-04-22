#!/usr/bin/env python3
"""Test only the newly added YAML files (Round 5)."""
import json
import time
import urllib.request

DEFAULT_MCP_URL = "http://127.0.0.1:8011/mcp"

NEW_FILES = [
    "Assets/Examples/Yaml/103-toggle-foldout.yaml",
    "Assets/Examples/Yaml/104-execute-command-and-validate.yaml",
    "Assets/Examples/Yaml/105-assert-property-element-traits.yaml",
    "Assets/Examples/Yaml/106-hover-duration.yaml",
    "Assets/Examples/Yaml/107-tab-view-select-index.yaml",
    "Assets/Examples/Yaml/108-two-pane-split-view-panes.yaml",
    "Assets/Examples/Yaml/_97-negative-drag-invalid-coordinates.yaml",
    "Assets/Examples/Yaml/_98-negative-toggle-foldout-invalid.yaml",
    "Assets/Examples/Yaml/_99-negative-execute-command-invalid.yaml",
    "Assets/Examples/Yaml/_100-negative-tab-view-invalid-index.yaml",
]


def call_tool(name: str, arguments: dict) -> dict:
    req = urllib.request.Request(DEFAULT_MCP_URL, method="POST")
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json, text/event-stream")
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": name, "arguments": arguments}}).encode()
    resp = urllib.request.urlopen(req, body, timeout=120)
    data = resp.read().decode()
    import re
    events = re.findall(r"data: (.+)", data)
    if events:
        return json.loads(events[0])
    return {}


def extract_text(result: dict) -> str:
    try:
        for c in result["result"]["content"]:
            if c.get("type") == "text":
                return c["text"]
    except Exception:
        pass
    return ""


def run_file(path: str) -> dict:
    print(f"\n>>> Running {path} ...")
    result = call_tool("unity_uiflow_run_file", {"yamlPath": path, "artifactDir": "D:/UnityUIFlow/artifacts", "exportZip": False, "stopOnFirstFailure": True})
    text = extract_text(result)
    print(f"    Result: {text[:500]}")
    try:
        data = json.loads(text)
        return data
    except Exception:
        return {"status": "unknown", "raw": text}


def main():
    results = []
    for f in NEW_FILES:
        data = run_file(f)
        result_data = data.get("data", {}).get("result", {})
        raw = result_data.get("raw", {})
        status = result_data.get("status", "unknown")
        passed = raw.get("passed", 0)
        failed = raw.get("failed", 0)
        errors = raw.get("errors", 0)
        is_negative = "-negative-" in f

        if is_negative:
            success = (failed > 0 or errors > 0)
        else:
            success = (status == "completed" and passed > 0 and failed == 0 and errors == 0)

        results.append({"file": f, "status": status, "passed": passed, "failed": failed, "errors": errors, "success": success, "is_negative": is_negative})
        time.sleep(1)

    print("\n" + "=" * 60)
    print("SUMMARY")
    print("=" * 60)
    for r in results:
        marker = "PASS" if r["success"] else "FAIL"
        neg_tag = " [NEG]" if r["is_negative"] else ""
        print(f"  {marker}{neg_tag}: {r['file']} (status={r['status']}, passed={r['passed']}, failed={r['failed']}, errors={r['errors']})")

    total_pass = sum(1 for r in results if r["success"])
    total = len(results)
    print(f"\nTotal: {total_pass}/{total}")


if __name__ == "__main__":
    main()
