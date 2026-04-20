import json
with open(r'D:\UnityUIFlow\Reports\McpImguiTest18\78f23bb3466343a38e5ba7b4df9f4660\Cases\IMGUI Example Window Smoke Test.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)
for step in data['StepResults']:
    idx = step['StepId']
    name = step['DisplayName']
    status = step['Status']
    print(f"Step {idx}: {name} - {status}")
    if status != 'Passed':
        print(f"  Error: {step['ErrorMessage']}")
