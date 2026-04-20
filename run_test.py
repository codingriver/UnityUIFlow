import asyncio
from mcp.client.streamable_http import streamablehttp_client
from mcp import ClientSession

async def run_test():
    async with streamablehttp_client(
        'http://127.0.0.1:8011/mcp',
        timeout=10,
        sse_read_timeout=1800,
        terminate_on_close=True,
    ) as (read, write, _):
        async with ClientSession(read, write) as session:
            await session.initialize()
            result = await session.call_tool('unity_uiflow_run_file', {
                'yamlPath': 'D:\\UnityUIFlow\\Assets\\Examples\\Yaml\\99-imgui-example.yaml',
                'artifactDir': 'D:\\UnityUIFlow\\Reports\\McpImguiTest20',
                'exportZip': True,
                'stopOnFirstFailure': True,
                'webhookOnFailure': True
            }, read_timeout_seconds=None)
            text = result.content[0].text
            import json
            data = json.loads(text)
            if data.get('ok') and data.get('data', {}).get('result', {}).get('cases'):
                case = data['data']['result']['cases'][0]
                print(f"Status: {case['status']}")
                print(f"Duration: {case['durationMs']}ms")
                for step in case['stepResults']:
                    print(f"  Step {step['stepIndex']}: {step['stepName']} = {step['status']}")
                    if step.get('errorMessage'):
                        print(f"    Error: {step['errorMessage']}")
            else:
                print(text)

asyncio.run(run_test())
