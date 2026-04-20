import asyncio
import json
import sys

async def reset():
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
            
            print("=== Force Reset UnityUIFlow ===")
            result = await session.call_tool('unity_uiflow_force_reset', {}, read_timeout_seconds=None)
            print(result.content[0].text)

if __name__ == "__main__":
    asyncio.run(reset())
