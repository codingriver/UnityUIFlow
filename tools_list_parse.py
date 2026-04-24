import re
with open(r'D:\UnityUIFlow\tools_list_raw.txt', 'r', encoding='utf-8') as f:
    content = f.read()
names = re.findall(r'"name":"([^"]+)"', content)
for n in names:
    print(n)
