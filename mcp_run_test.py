import asyncio
import json
import sys
import time

async def run_test():
    try:
        from mcp import ClientSession
        from mcp.client.streamable_http import streamablehttp_client
    except ImportError:
        print("ERROR: mcp package not installed")
        sys.exit(1)

    async with streamablehttp_client(
        'http://127.0.0.1:8011/mcp',
        timeout=10,
        sse_read_timeout=1800,
        terminate_on_close=True,
    ) as (read, write, _):
        async with ClientSession(read, write) as session:
            await session.initialize()
            
            # Step 1: Check MCP status
            print("=== Checking MCP Status ===")
            result = await session.call_tool('unity_mcp_status', {}, read_timeout_seconds=None)
            status_text = result.content[0].text
            print(status_text)
            
            status_data = json.loads(status_text)
            data = status_data.get("data", {})
            unity_connected = data.get("connected", False) and data.get("serverReady", False)
            
            if not unity_connected:
                print("ERROR: Unity is not connected to MCP server.")
                print("Please open Unity Editor and ensure the UnityPilot bridge is connected.")
                sys.exit(1)
            
            # Step 2: Run the YAML test
            print("\n=== Running IMGUI Example Window Smoke Test ===")
            result = await session.call_tool('unity_uiflow_run_file', {
                "yamlPath": "Assets/Examples/Yaml/99-imgui-example.yaml",
                "headed": True,
                "reportOutputPath": "Reports/McpImguiTest",
                "screenshotOnFailure": True,
                "stopOnFirstFailure": True,
                "enableVerboseLog": True,
                "defaultTimeoutMs": 15000
            }, read_timeout_seconds=None)
            
            result_text = result.content[0].text
            print(result_text)
            
            # Try to parse result
            try:
                result_data = json.loads(result_text)
                if result_data.get("ok") == True:
                    inner = result_data.get("data", {})
                    raw = inner.get("raw", {})
                    case_status = raw.get("status", "")
                    cases = raw.get("cases", [])
                    if cases:
                        case = cases[0]
                        step_results = case.get("stepResults", [])
                        print("\n=== Step Results ===")
                        for sr in step_results:
                            status = sr.get("status", "")
                            name = sr.get("stepName", "")
                            duration = sr.get("durationMs", 0)
                            err = sr.get("errorCode", "")
                            print(f"  [{status}] {name} ({duration}ms) {err}")
                        
                        if case.get("status") == "Passed":
                            print("\n✅ Test passed!")
                            return
                        else:
                            print(f"\n❌ Test failed! Case status: {case.get('status')}")
                            sys.exit(1)
                    else:
                        print("\n⚠️ No case results found.")
                        sys.exit(1)
                else:
                    print("\n❌ MCP call failed!")
                    sys.exit(1)
            except Exception as e:
                print(f"\n⚠️ Parse error: {e}")
                if "failed" in result_text.lower() or "error" in result_text.lower():
                    print("\n❌ Test may have failed. Check output above.")
                    sys.exit(1)
                else:
                    print("\n✅ Test appears to have passed.")

if __name__ == "__main__":
    asyncio.run(run_test())
