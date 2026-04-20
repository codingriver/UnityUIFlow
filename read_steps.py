import json
with open(r'D:\UnityUIFlow\Reports\UnityUIFlowMcp\c45176dec50445e2b8afc3d978bffeff\Cases\IMGUI Example Window Smoke Test.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)
for step in data['StepResults']:
    print(f"Step {step['StepIndex']}: {step.get('StepName','')} = {step['Status']}")
    if step.get('ErrorMessage'):
        print(f"  Error: {step['ErrorMessage']}")
