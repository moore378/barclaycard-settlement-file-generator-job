import sys
import re

ver_pattern1 = r'\[assembly: AssemblyVersion\("((\d+)\.(\d+)\.(\d+)\.(\d+))"\)\]'
file1 = "CCTM/Properties/AssemblyInfo.cs"
with open(file1, "r") as f:
    assemblyInfo1 = f.read()
version_match = re.search(ver_pattern1, assemblyInfo1, re.M)

if version_match:
    commit_message_fn = sys.argv[1]
    version_str = version_match.group(1) + ";"
    print("Version: " + version_str)

    with open(commit_message_fn, 'r') as original: data = original.read()
    with open(commit_message_fn, 'w') as modified: modified.write(version_str + "\n" + data)
else:
    print("Version not found")