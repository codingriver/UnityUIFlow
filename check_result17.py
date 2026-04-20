import json
with open(r'D:\UnityUIFlow\Reports\McpImguiTest17\93dce1d63a554d859eba93d52a9bfa3d\Cases\IMGUI Example Window Smoke Test.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)
for step in data['StepResults']:
    idx = step['StepIndex']
    name = step['StepName']
    status = step['Status']
    print(f"Step {idx}: {name} - {status}")
    if status != 'Passed':
        print(f"  Error: {step['ErrorMessage']}")
