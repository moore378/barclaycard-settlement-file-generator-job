import re
import subprocess

file1 = "RTCC/Properties/AssemblyInfo.cs"
file2 = "CCTM/Properties/AssemblyInfo.cs"
with open(file1, "r") as f:
    assemblyInfo1 = f.read()
with open(file2, "r") as f:
    assemblyInfo2 = f.read()

ver_pattern1 = r'\[assembly: AssemblyVersion\("((\d+)\.(\d+)\.(\d+)\.(\d+))"\)\]'
tar_pattern1 = r'[assembly: AssemblyVersion("\2.\3.%i.0")]'
ver_pattern2 = r'\[assembly: AssemblyFileVersion\("((\d+)\.(\d+)\.(\d+)\.(\d+))"\)\]'
tar_pattern2 = r'[assembly: AssemblyFileVersion("\2.\3.%i.0")]'
version1 = re.search(ver_pattern1, assemblyInfo1, re.M)
version2 = re.search(ver_pattern1, assemblyInfo2, re.M)

if not version1 or not version2:
    print("Could not find version")
    exit(1)

if version1.group(1) != version2.group(1):
    print("Versions don't match: " + version1.group(1) + " and " + version2.group(1))

majorNum = int(version1.group(2))
minorNum = int(version1.group(3))
patchNum = int(version1.group(4)) + 1
buildNum = int(version1.group(5))

assemblyInfo1 = re.sub(ver_pattern1, tar_pattern1 % patchNum, assemblyInfo1)
assemblyInfo1 = re.sub(ver_pattern2, tar_pattern2 % patchNum, assemblyInfo1)
assemblyInfo2 = re.sub(ver_pattern1, tar_pattern1 % patchNum, assemblyInfo2)
assemblyInfo2 = re.sub(ver_pattern2, tar_pattern2 % patchNum, assemblyInfo2)

with open(file1, "w") as f:
    f.write(assemblyInfo1)
with open(file2, "w") as f:
    f.write(assemblyInfo2)

subprocess.check_output("msbuild RTCC\RTCC.csproj")
subprocess.check_output("msbuild CCTM\CCTM.csproj")