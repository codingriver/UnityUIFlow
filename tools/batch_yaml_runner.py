#!/usr/bin/env python3
"""
UnityUIFlow Batch YAML Runner — Agent-side batch executor via MCP HTTP.

Usage:
    python batch_yaml_runner.py --yaml-dir Assets/Examples/Yaml --batch-size 10 --report-dir Reports/AgentBatch
    python batch_yaml_runner.py --retry-from Reports/AgentBatch/batch_002_failed.json
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import time
import urllib.request
from pathlib import Path
from typing import Any

DEFAULT_MCP_URL = "http://127.0.0.1:8011/mcp"


class McpHttpClient:
    def __init__(self, url: str = DEFAULT_MCP_URL):
        self.url = url
        self._req_counter = 0

    def _next_id(self) -> int:
        self._req_counter += 1
        return self._req_counter

    def call(self, method: str, params: dict | None = None) -> dict[str, Any]:
        req = urllib.request.Request(self.url, method="POST")
        req.add_header("Content-Type", "application/json")
        req.add_header("Accept", "application/json, text/event-stream")
        body = json.dumps({
            "jsonrpc": "2.0",
            "id": self._next_id(),
            "method": method,
            "params": params or {},
        }).encode()
        resp = urllib.request.urlopen(req, body, timeout=600)
        data = resp.read().decode()
        events = re.findall(r"data: (.+)", data)
        if events:
            return json.loads(events[0])
        return {}

    def call_tool(self, name: str, arguments: dict | None = None) -> dict[str, Any]:
        return self.call("tools/call", {"name": name, "arguments": arguments or {}})

    def ensure_ready(self, timeout_s: float = 120) -> bool:
        deadline = time.monotonic() + timeout_s
        while time.monotonic() < deadline:
            result = self.call_tool("unity_mcp_status", {})
            text = self._extract_text(result)
            try:
                data = json.loads(text)
                if data.get("ok") and data.get("data", {}).get("connected"):
                    return True
            except Exception:
                pass
            time.sleep(2.0)
        return False

    @staticmethod
    def _extract_text(result: dict) -> str:
        try:
            contents = result["result"]["content"]
            for c in contents:
                if c.get("type") == "text":
                    return c["text"]
        except Exception:
            pass
        return ""


def discover_yaml_files(yaml_dir: str) -> list[str]:
    p = Path(yaml_dir)
    files = sorted([str(f.resolve()) for f in p.glob("*.yaml")])
    return files


def is_negative_test(yaml_path: str) -> bool:
    return "-negative-" in Path(yaml_path).name.lower()


def run_batch(
    client: McpHttpClient,
    yaml_paths: list[str],
    report_dir: str,
    headed: bool = True,
    stop_on_first_failure: bool = False,
    default_timeout_ms: int = 10000,
) -> dict[str, Any]:
    print(f"[Batch] Running {len(yaml_paths)} file(s) -> {report_dir}")
    for yp in yaml_paths:
        marker = " [NEG]" if is_negative_test(yp) else ""
        print(f"  - {yp}{marker}")

    has_negative = any(is_negative_test(yp) for yp in yaml_paths)
    continue_on_step_failure = has_negative  # allow negative tests to run all steps

    result = client.call_tool(
        "unity_uiflow_run_batch",
        {
            "yamlPaths": yaml_paths,
            "batchSize": len(yaml_paths),
            "batchOffset": 0,
            "headed": headed,
            "reportOutputPath": report_dir,
            "screenshotOnFailure": True,
            "stopOnFirstFailure": stop_on_first_failure,
            "continueOnStepFailure": continue_on_step_failure,
            "defaultTimeoutMs": default_timeout_ms,
            "enableVerboseLog": True,
            "debugOnFailure": False,
        },
    )
    return result


def parse_batch_result(result: dict, yaml_paths: list[str]) -> dict[str, Any]:
    text = McpHttpClient._extract_text(result)
    try:
        payload = json.loads(text)
    except Exception as e:
        return {"ok": False, "error": f"Failed to parse result: {e}", "raw": text}

    if not payload.get("ok"):
        return {"ok": False, "error": payload.get("error", {}).get("message", "Unknown error"), "raw": payload}

    data = payload.get("data", {})
    result_data = data.get("result", {})

    # Reconcile negative tests: case-level Failed is expected
    negative_paths = {yp for yp in yaml_paths if is_negative_test(yp)}
    raw_cases = result_data.get("raw", {}).get("cases", [])
    adjusted_passed = result_data.get("passed", 0)
    adjusted_failed = result_data.get("failed", 0)
    negative_expected_failures = 0

    for case in raw_cases:
        case_name = case.get("caseName", "")
        case_status = case.get("status", "")
        # Find matching yaml path by case name heuristics
        matched_yaml = None
        for yp in yaml_paths:
            if case_name.replace(" ", "-").lower() in yp.lower() or Path(yp).stem.replace("-", " ").lower() in case_name.lower():
                matched_yaml = yp
                break
        if matched_yaml and is_negative_test(matched_yaml):
            if case_status == "Failed":
                # Expected failure -> count as pass
                adjusted_passed += 1
                adjusted_failed = max(0, adjusted_failed - 1)
                negative_expected_failures += 1
            elif case_status == "Passed":
                # Negative test unexpectedly passed -> count as fail
                adjusted_passed = max(0, adjusted_passed - 1)
                adjusted_failed += 1

    return {
        "ok": True,
        "status": result_data.get("status"),
        "total": result_data.get("total", 0),
        "passed": adjusted_passed,
        "failed": adjusted_failed,
        "errors": result_data.get("errors", 0),
        "skipped": result_data.get("skipped", 0),
        "negativeExpectedFailures": negative_expected_failures,
        "reportPath": data.get("reportOutputPath", ""),
        "screenshotPath": data.get("screenshotPath", ""),
        "raw": result_data.get("raw", {}),
    }


def save_failed_manifest(path: Path, yaml_paths: list[str], batch_idx: int, report_dir: str, raw: dict) -> None:
    manifest = {
        "batchIndex": batch_idx,
        "yamlPaths": yaml_paths,
        "reportDir": report_dir,
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%S"),
        "rawResult": raw,
    }
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[Batch] Failed manifest saved to {path}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Batch YAML test runner for UnityUIFlow")
    parser.add_argument("--yaml-dir", default="Assets/Examples/Yaml", help="Directory containing YAML test files")
    parser.add_argument("--batch-size", type=int, default=10, help="Files per batch")
    parser.add_argument("--report-dir", default="Reports/AgentBatch", help="Base report directory")
    parser.add_argument("--headed", type=lambda x: x.lower() in ("1", "true", "yes"), default=True, help="Run in headed mode")
    parser.add_argument("--mcp-url", default=DEFAULT_MCP_URL, help="MCP HTTP endpoint")
    parser.add_argument("--retry-from", default="", help="Path to a failed-manifest JSON to retry")
    parser.add_argument("--stop-on-first-failure", action="store_true", help="Stop immediately on first failure")
    parser.add_argument("--timeout-ms", type=int, default=10000, help="Default step timeout in ms")
    parser.add_argument("--wait-ready", type=int, default=120, help="Seconds to wait for Unity ready")
    args = parser.parse_args()

    client = McpHttpClient(args.mcp_url)

    print("[Setup] Waiting for Unity to be ready...")
    if not client.ensure_ready(timeout_s=args.wait_ready):
        print("[Error] Unity is not connected. Please start Unity Editor and ensure the Bridge is active.")
        return 2
    print("[Setup] Unity ready.")

    if args.retry_from:
        manifest_path = Path(args.retry_from)
        if not manifest_path.exists():
            print(f"[Error] Retry manifest not found: {manifest_path}")
            return 2
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        all_files = manifest["yamlPaths"]
        base_report_dir = manifest["reportDir"]
        start_batch = manifest["batchIndex"]
        print(f"[Retry] Resuming batch {start_batch} with {len(all_files)} file(s)")
    else:
        all_files = discover_yaml_files(args.yaml_dir)
        if not all_files:
            print(f"[Error] No YAML files found in {args.yaml_dir}")
            return 2
        base_report_dir = args.report_dir
        start_batch = 0
        print(f"[Setup] Discovered {len(all_files)} YAML file(s)")

    overall_passed = 0
    overall_failed = 0
    overall_errors = 0
    failed_batches: list[Path] = []

    for i in range(start_batch, (len(all_files) + args.batch_size - 1) // args.batch_size):
        offset = i * args.batch_size
        batch_files = all_files[offset:offset + args.batch_size]
        batch_report_dir = f"{base_report_dir}/batch_{i:03d}"

        mcp_result = run_batch(
            client,
            batch_files,
            batch_report_dir,
            headed=args.headed,
            stop_on_first_failure=args.stop_on_first_failure,
            default_timeout_ms=args.timeout_ms,
        )
        parsed = parse_batch_result(mcp_result, batch_files)

        if not parsed["ok"]:
            print(f"[Batch {i}] MCP call failed: {parsed.get('error')}")
            manifest_path = Path(f"{base_report_dir}/batch_{i:03d}_failed.json")
            save_failed_manifest(manifest_path, batch_files, i, batch_report_dir, parsed.get("raw", {}))
            failed_batches.append(manifest_path)
            if args.stop_on_first_failure:
                return 1
            continue

        neg_info = f" (neg_expected={parsed.get('negativeExpectedFailures', 0)})" if parsed.get('negativeExpectedFailures', 0) > 0 else ""
        print(f"[Batch {i}] Status={parsed['status']} total={parsed['total']} passed={parsed['passed']} failed={parsed['failed']} errors={parsed['errors']} skipped={parsed['skipped']}{neg_info}")
        overall_passed += parsed["passed"]
        overall_failed += parsed["failed"]
        overall_errors += parsed["errors"]

        if parsed["status"] != "completed" or parsed["failed"] > 0 or parsed["errors"] > 0:
            manifest_path = Path(f"{base_report_dir}/batch_{i:03d}_failed.json")
            save_failed_manifest(manifest_path, batch_files, i, batch_report_dir, parsed.get("raw", {}))
            failed_batches.append(manifest_path)
            if args.stop_on_first_failure:
                return 1

    summary = {
        "totalFiles": len(all_files),
        "totalPassed": overall_passed,
        "totalFailed": overall_failed,
        "totalErrors": overall_errors,
        "failedBatches": [str(p) for p in failed_batches],
        "baseReportDir": base_report_dir,
    }
    summary_path = Path(f"{base_report_dir}/summary.json")
    summary_path.parent.mkdir(parents=True, exist_ok=True)
    summary_path.write_text(json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8")

    print("\n" + "=" * 60)
    print(f"Total files : {len(all_files)}")
    print(f"Passed      : {overall_passed}")
    print(f"Failed      : {overall_failed}")
    print(f"Errors      : {overall_errors}")
    print(f"Summary     : {summary_path}")
    if failed_batches:
        print(f"Failed batches ({len(failed_batches)}):")
        for p in failed_batches:
            print(f"  - {p}")
        print("\nTo retry a failed batch:")
        print(f"  python batch_yaml_runner.py --retry-from {failed_batches[0]}")
        return 1
    print("=" * 60)
    return 0


if __name__ == "__main__":
    sys.exit(main())
