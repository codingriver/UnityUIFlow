import asyncio
import json
import sys

async def call_mcp():
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
            tools = await session.list_tools()
            print("=== Available Tools ===")
            for tool in tools.tools:
                print(f"Tool: {tool.name}")
                print(f"  Description: {tool.description}")
                print(f"  Schema: {tool.inputSchema}")
                print()
            return tools

if __name__ == "__main__":
    asyncio.run(call_mcp())
