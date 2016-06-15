@echo off
del signroot.* /q /s
makecert.exe -sv SignRoot.pvk -cy authority -r signroot.cer -a sha256 -n "CN=Dev Certification Authority" -ss my -sr localmachine