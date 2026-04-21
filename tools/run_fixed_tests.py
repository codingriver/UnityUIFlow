#!/usr/bin/env python3
"""Run the three fixed test files to verify fixes."""

import sys
import json
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from batch_yaml_runner import (
    McpHttpClient,
    run_batch,
    parse_batch_result,
    is_negative_test,
)

DEFAULT_MCP_URL = "http://127.0.0.1:8011/mcp"


def main() -> int:
    test_files = [
        "D:\\UnityUIFlow\\Assets\\Examples\\Yaml\\93-host-window-advanced.yaml",
        "D:\\UnityUIFlow\\Assets\\Examples\\Yaml\\98-imgui-advanced.yaml",
        "D:\\UnityUIFlow\\Assets\\Examples\\Yaml\\sample-04-conditional-and-loop.yaml",
    ]

    client = McpHttpClient(DEFAULT_MCP_URL)
    print("[Setup] Waiting for Unity to be ready...")
    if not client.ensure_ready(timeout_s=120):
        print("[Error] Unity not ready")
        return 2
    print("[Setup] Unity ready.")

    print(f"\n[Fixed Tests] Running {len(test_files)} file(s)...")
    for yp in test_files:
        marker = " [NEG]" if is_negative_test(yp) else ""
        print(f"  - {yp}{marker}")

    mcp_result = run_batch(
        client,
        test_files,
        "Reports/AgentBatch_FixedTests",
        headed=True,
        stop_on_first_failure=False,
        default_timeout_ms=15000,
    )
    parsed = parse_batch_result(mcp_result, test_files)

    if not parsed["ok"]:
        print(f"[Fixed Tests] MCP call failed: {parsed.get('error')}")
        return 1

    print(
        f"[Fixed Tests] Status={parsed['status']} total={parsed['total']} "
        f"passed={parsed['passed']} failed={parsed['failed']} "
        f"errors={parsed['errors']} skipped={parsed['skipped']}"
    )

    return 0


if __name__ == "__main__":
    sys.exit(main())
