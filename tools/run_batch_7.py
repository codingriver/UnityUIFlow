#!/usr/bin/env python3
"""Run batch 7 (last batch)."""

import sys
import json
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from batch_yaml_runner import (
    McpHttpClient,
    run_batch,
    parse_batch_result,
    discover_yaml_files,
    is_negative_test,
)

DEFAULT_MCP_URL = "http://127.0.0.1:8011/mcp"
BATCH_SIZE = 10
REPORT_DIR = "Reports/AgentBatch_v6"


def main() -> int:
    all_files = discover_yaml_files("Assets/Examples/Yaml")
    batch_files = all_files[70:]  # batch 7 (last file)
    batch_report_dir = f"{REPORT_DIR}/batch_007"

    client = McpHttpClient(DEFAULT_MCP_URL)
    print("[Setup] Waiting for Unity to be ready...")
    if not client.ensure_ready(timeout_s=120):
        print("[Error] Unity not ready")
        return 2
    print("[Setup] Unity ready.")

    print(f"\n[Batch 7] Running {len(batch_files)} file(s)...")
    for yp in batch_files:
        marker = " [NEG]" if is_negative_test(yp) else ""
        print(f"  - {yp}{marker}")

    mcp_result = run_batch(
        client,
        batch_files,
        batch_report_dir,
        headed=True,
        stop_on_first_failure=False,
        default_timeout_ms=15000,
    )
    parsed = parse_batch_result(mcp_result, batch_files)

    if not parsed["ok"]:
        print(f"[Batch 7] MCP call failed: {parsed.get('error')}")
        return 1

    neg_info = ""
    if parsed.get("negativeExpectedFailures", 0) > 0:
        neg_info = f" (neg_expected={parsed['negativeExpectedFailures']})"

    print(
        f"[Batch 7] Status={parsed['status']} total={parsed['total']} "
        f"passed={parsed['passed']} failed={parsed['failed']} "
        f"errors={parsed['errors']} skipped={parsed['skipped']}{neg_info}"
    )

    return 0


if __name__ == "__main__":
    sys.exit(main())
