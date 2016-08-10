import zipfile
import re
import shutil
import subprocess
import webbrowser
import sys
import urllib.parse
import os


if __name__ == '__main__':
    zipf = None
    zipFileName = "deploy"
    version = ""
    copy_to = "."
    version_info = ""
    version_info_regex = ""
    notes_file = ""
    mail_to = "alex.schwarz@ipsgroupinc.com"

    for fn in open('..\..\deploy-files.txt', 'r'):
        fn = fn.strip()
        # variable?
        match = re.match(r"^(.+?)=(.+)$", fn)
        if match:
            variableName = match.group(1)
            args = match.group(2)
            if variableName == "version":
                args = re.match(r"^(.+?)=(.+)$", args)
                if not args:
                    sys.exit(1)
                versionFileName = args.group(1)
                versionRegex = args.group(2)
                versionFile = open(versionFileName, "r").read()
                # print(versionFile)
                versionMatch = re.search(versionRegex, versionFile, re.MULTILINE)
                if not versionMatch:
                    print("Version not found in file " + versionRegex)
                    sys.exit(1)
                version_info = versionMatch.group(0)
                version_info_regex = versionRegex
                version = versionMatch.group(1)
                print("version=" + version)
            elif variableName == "copy-to":
                copy_to = args
                print("copy-to=" + copy_to)
            elif variableName == "notes-file":
                notes_file = args
                print("notes-file=" + notes_file)
            elif variableName == "mail-to":
                mail_to = args
                print("mail-to=" + mail_to)
            elif variableName == "filename":
                if zipf:
                    print("filename needs to be specified before any files")
                    sys.exit(1)
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
        body = open(notes_file, "r").read() if notes_file else ""
        # truncate the body to a small size if needed to ensure the body
        # message can be composed.
        body = os.path.join(copy_to, zipFileName) + "\n\n" + body[:1024]
        webbrowser.open("mailto:%s?subject=%s&body=%s" % (
            urllib.parse.quote(mail_to),
            urllib.parse.quote(zipFileName),
            urllib.parse.quote(body)))
