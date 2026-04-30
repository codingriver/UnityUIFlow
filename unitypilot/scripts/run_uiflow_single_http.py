import asyncio
from datetime import timedelta
from mcp import ClientSession
from mcp.client.streamable_http import streamablehttp_client

MCP_URL = 'http://127.0.0.1:8011/mcp'
YAML_PATH = 'D:/UnityUIFlow/Assets/Examples/Yaml/13-advanced-controls.yaml'

async def call_tool(session, name, args, read_timeout_seconds=None):
    timeout = None if read_timeout_seconds is None else timedelta(seconds=read_timeout_seconds)
    result = await session.call_tool(name, args, read_timeout_seconds=timeout)
    print(f'\n=== {name} ===')
    if result.content:
        for item in result.content:
            text = getattr(item, 'text', None)
            if text:
                print(text)
            else:
                print(item)
    else:
        print('(no content)')
    return result

async def main():
    async with streamablehttp_client(MCP_URL, timeout=10, sse_read_timeout=1800, terminate_on_close=True) as (read, write, _):
        async with ClientSession(read, write) as session:
            await session.initialize()
            await call_tool(session, 'unity_sync_after_disk_write', {
                'delayS': 2,
                'triggerCompile': True,
            }, read_timeout_seconds=90)
            await call_tool(session, 'unity_compile_wait', {
                'timeoutS': 60,
                'pollIntervalS': 1.0,
                'preferEvents': True,
            }, read_timeout_seconds=90)
            await call_tool(session, 'unity_uiflow_run_file', {
                'yamlPath': YAML_PATH,
                'headed': True,
                'reportOutputPath': 'Reports/UnityUIFlowMcp',
                'screenshotPath': 'Reports/UnityUIFlowMcp/Screenshots',
                'screenshotOnFailure': True,
                'stopOnFirstFailure': True,
                'continueOnStepFailure': False,
                'debugOnFailure': False,
                'defaultTimeoutMs': 10000,
                'preStepDelayMs': 0,
                'enableVerboseLog': True,
            }, read_timeout_seconds=None)

asyncio.run(main())
