import pathlib, sys, datetime

logs = sorted(pathlib.Path(r'D:\UnityUIFlow\Logs').glob('Editor*.log'), key=lambda p: p.stat().st_mtime, reverse=True)
if not logs:
    print("No logs found")
    sys.exit(0)

log = logs[0]
print(f"=== {log.name} (modified {datetime.datetime.fromtimestamp(log.stat().st_mtime)}) ===")
with open(log, 'r', encoding='utf-8', errors='ignore') as f:
    lines = f.readlines()

# Find lines around test run time (17:01:38)
patterns = ['NullReferenceException', 'Object reference', 'imgui_focus', 'IMGUI command failed', 'Fallback snapshot']
for i, line in enumerate(lines):
    if any(p in line for p in patterns):
        print(f"{i+1}: {line.rstrip()}")
print()
