#!/usr/bin/env python3
"""Run remaining UnityUIFlow YAML batches 5-7 (retry after compile fix)."""

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
START_BATCH = 5
REPORT_DIR = "Reports/AgentBatch_v3"


def main() -> int:
    all_files = discover_yaml_files("Assets/Examples/Yaml")
    total_batches = (len(all_files) + BATCH_SIZE - 1) // BATCH_SIZE

    client = McpHttpClient(DEFAULT_MCP_URL)
    print("[Setup] Waiting for Unity to be ready...")
    if not client.ensure_ready(timeout_s=120):
        print("[Error] Unity not ready")
        return 2
    print("[Setup] Unity ready.")

    for i in range(START_BATCH, total_batches):
        offset = i * BATCH_SIZE
        batch_files = all_files[offset : offset + BATCH_SIZE]
        batch_report_dir = f"{REPORT_DIR}/batch_{i:03d}"

        print(f"\n[Batch {i}] Running {len(batch_files)} file(s)...")
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
            print(f"[Batch {i}] MCP call failed: {parsed.get('error')}")
            continue

        neg_info = ""
        if parsed.get("negativeExpectedFailures", 0) > 0:
            neg_info = f" (neg_expected={parsed['negativeExpectedFailures']})"

        print(
            f"[Batch {i}] Status={parsed['status']} total={parsed['total']} "
            f"passed={parsed['passed']} failed={parsed['failed']} "
            f"errors={parsed['errors']} skipped={parsed['skipped']}{neg_info}"
        )

        # Count negative tests in batch - if all failures are negative tests, continue
        negative_count = sum(1 for yp in batch_files if is_negative_test(yp))
        if parsed["failed"] == negative_count and parsed["errors"] == 0:
            print(f"[Batch {i}] All failures are expected negative tests. Continuing.")
            continue

        if (
            parsed["status"] != "completed"
            or parsed["failed"] > negative_count
            or parsed["errors"] > 0
        ):
            print(f"[Batch {i}] Batch had unexpected failures/errors. Stopping for review.")
            break

    print("\nDone.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
