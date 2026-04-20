import pathlib, sys

logs = sorted(pathlib.Path(r'D:\UnityUIFlow\Logs').glob('Editor*.log'), key=lambda p: p.stat().st_mtime, reverse=True)
if not logs:
    print("No logs found")
    sys.exit(0)

for log in logs[:2]:
    print(f"=== {log.name} ===")
    with open(log, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    patterns = ['OnGUIReplacement', 'imgui_click', 'imgui_assert_text', 'SendMouseEvent', 'Invalid GUILayout', 'OnGenerateClicked', 'Status: Ready', 'Status: Generated', 'IMGUI command failed', 'Fallback snapshot']
    for i, line in enumerate(lines):
        line_lower = line.lower()
        if any(p.lower() in line_lower for p in patterns):
            print(f"{i+1}: {line.rstrip()}")
    print()
