#!/usr/bin/env python
from __future__ import with_statement
import os
from contextlib import closing
from zipfile import ZipFile, ZIP_DEFLATED
import re
import subprocess


def ziprelease(silent=False):
    basedir = "./Legendary Rune Maker/bin/Release"

    assert os.path.isdir(basedir)
    with closing(ZipFile("Release.zip", "w", ZIP_DEFLATED)) as z:
        for root, dirs, files in os.walk(basedir):
            # NOTE: ignore empty directories
            for fn in files:
                absfn = os.path.join(root, fn)
                zfn = absfn[len(basedir) + len(os.sep):]  # relative path

                if (zfn.startswith("cache") or zfn.endswith(".json") or
                        zfn.endswith(".xml")):
                    continue

                if (not silent):
                    print(zfn)

                z.write(absfn, zfn)


def getVersion():
    return [x for x in
            [re.findall(r"(?<=AssemblyVersion\(\").*(?=\")", line) for line in open("./Legendary Rune Maker/Properties/AssemblyInfo.cs")]
            if len(x) == 1][0][0]


def setSetupVersion(version):
    with open('./setup/script.iss', 'r') as f:
        content = f.read()
        content = re.sub(r'(?<=MyAppVersion \").*(?=\")', version, content, flags = re.M)

    with open('./setup/script.iss', 'w') as f:
        f.write(content)


print("Building version " + getVersion() + "...")

code = os.system("msbuild /p:Configuration=Release /v:m")

if (code != 0):
    print("Build failed!")
    input("Press Enter to continue...")
    exit()

print("Packing...")
ziprelease(True)

print("Setting version on InnoSetup script...")
setSetupVersion(getVersion())

print("Compiling setup...")
subprocess.run("C:/Program Files (x86)/Inno Setup 5/ISCC.exe setup/script.iss")

print()
input("Done")