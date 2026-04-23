import json, http.client, re

conn = http.client.HTTPConnection('127.0.0.1', 8011, timeout=10)

body1 = json.dumps({
    'jsonrpc': '2.0', 'id': 1,
    'method': 'initialize',
    'params': {
        'protocolVersion': '2024-11-05',
        'capabilities': {},
        'clientInfo': {'name': 'cli', 'version': '1.0.0'}
    }
})
conn.request('POST', '/mcp', body=body1, headers={
    'Accept': 'application/json, text/event-stream',
    'Content-Type': 'application/json'
})
resp1 = conn.getresponse()
_ = resp1.read()

body2 = json.dumps({
    'jsonrpc': '2.0', 'id': 2,
    'method': 'tools/list',
    'params': {}
})
conn.request('POST', '/mcp', body=body2, headers={
    'Accept': 'application/json, text/event-stream',
    'Content-Type': 'application/json'
})
resp2 = conn.getresponse()
data2 = resp2.read().decode()

found = False
for line in data2.splitlines():
    if line.startswith('data:'):
        payload = line[5:].strip()
        try:
            obj = json.loads(payload)
            if 'result' in obj and 'tools' in obj['result']:
                tools = obj['result']['tools']
                found = True
                print('=== MCP Server Tools (' + str(len(tools)) + ' total) ===')
                print('')
                for t in tools:
                    desc = t.get('description', '')
                    if len(desc) > 80:
                        desc = desc[:80] + '...'
                    print('  ' + t['name'])
                    print('    ' + desc)
                    print('')
        except Exception:
            pass

conn.close()

if not found:
    print('Could not parse tools from MCP response, falling back to source scan...')
    print('')
    with open('D:/unitypilot/src/unitypilot_mcp/mcp_stdio_server.py', 'r', encoding='utf-8') as f:
        src = f.read()
    pattern = r'@mcp\.tool\([^)]*description\s*=\s*"([^"]*)"[^)]*\)\s*\nasync def ([a-zA-Z0-9_]+)'
    tools = re.findall(pattern, src)
    print('=== MCP Server Tools (from source) ===')
    print('')
    for desc, name in tools:
        short = desc.replace('\n', ' ').strip()
        if len(short) > 80:
            short = short[:80] + '...'
        print('  ' + name)
        print('    ' + short)
        print('')
