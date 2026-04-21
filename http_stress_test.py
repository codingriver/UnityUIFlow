import asyncio
import http.client

async def make_request(req_id):
    try:
        loop = asyncio.get_running_loop()
        def _sync_request():
            conn = http.client.HTTPConnection("127.0.0.1", 8011, timeout=5)
            try:
                headers = {
                    "Accept": "application/json,text/event-stream",
                    "Content-Type": "application/json",
                }
                body = b'{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}'
                conn.request("POST", "/mcp", body=body, headers=headers)
                resp = conn.getresponse()
                data = resp.read()
                return resp.status, len(data)
            finally:
                conn.close()
        status, length = await asyncio.wait_for(asyncio.to_thread(_sync_request), timeout=6)
        print(f"Request {req_id}: status={status}, len={length}")
    except Exception as e:
        print(f"Request {req_id}: error={type(e).__name__}: {e}")

async def main():
    # Batch 1: normal requests
    tasks = [make_request(i) for i in range(10)]
    await asyncio.gather(*tasks)
    print("Batch 1 done")
    
    # Batch 2: fire-and-forget (simulate client disconnect)
    for i in range(10, 20):
        task = asyncio.create_task(make_request(i))
        await asyncio.sleep(0.05)
        task.cancel()
    print("Batch 2 done")
    
    # Batch 3: verify still works
    tasks = [make_request(i) for i in range(20, 25)]
    await asyncio.gather(*tasks)
    print("Batch 3 done")

asyncio.run(main())
