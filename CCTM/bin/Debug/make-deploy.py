import os
import zipfile
import re
import shutil
import subprocess

if __name__ == '__main__':
    zipf = None
    zipFileName = "deploy"
    version = ""
    copy_to = "."
    version_info = ""
    version_info_regex  = ""
    
    for fn in open('deploy-files.txt', 'r'):
        fn = fn.strip()
        # variable?
        match = re.match(r"^(.+?)=(.+)$", fn)
        if match:
            variableName = match.group(1)
            args = match.group(2)
            if variableName == "version":
                args = re.match(r"^(.+?)=(.+)$", args)
                if not args:
                    exit(1)
                versionFileName = args.group(1)
                versionRegex = args.group(2)
                versionFile = open(versionFileName, "r").read()
                # print(versionFile)
                versionMatch = re.search(versionRegex, versionFile, re.MULTILINE)
                if not versionMatch:
                    print("Version not found in file " + versionRegex)
                    exit(1)
                version_info = versionMatch.group(0)
                version_info_regex = versionRegex
                version = versionMatch.group(1)
                print("version=" + version)
            elif variableName == "copy-to":
                copy_to = args
                print("copy-to=" + copy_to)
            elif variableName == "filename":
                if zipf:
                    print("filename needs to be specified before any files")
                    exit(1)
                zipFileName = re.sub(version_info_regex, args, version_info)
                print("filename=" + zipFileName)
            else:
                results = subprocess.check_output(args).decode("utf-8")
                fn = variableName
                with open(fn, "w") as f:
                    f.write(results)
                print("Zipping file " + fn)
                if not zipf:
                    zipf = zipfile.ZipFile(zipFileName, 'w')
                zipf.write(fn)
        else:
            print("Zipping file " + fn)
            if not zipf:
                zipf = zipfile.ZipFile(zipFileName, 'w')
            zipf.write(fn)
    if zipf:
        zipf.close()
        if copy_to != ".":
            shutil.copy(zipFileName, copy_to)