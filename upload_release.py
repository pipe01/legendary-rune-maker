#!/usr/bin/env python
from __future__ import with_statement
import ftplib
import re
import os
import sys
from contextlib import closing
from zipfile import ZipFile, ZIP_DEFLATED


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
                        zfn.endswith(".xml") or zfn.endswith(".pdb")):
                    continue

                if (not silent):
                    print(zfn)

                z.write(absfn, zfn)


def clean():
    if (os._exists("updates.man")):
        os.remove("updates.man")
    if (os._exists("Release.zip")):
        os.remove("Release.zip")


print("Building release configuration...")
code = os.system("msbuild /p:Configuration=Release /v:m")

if (code != 0):
    print("Build failed!")
    input("Press Enter to continue...")
    clean()
    exit()

versionRegex = re.compile("\\d+\\.\\d+\\.\\d+")

with open("./Legendary Rune Maker/Properties/AssemblyInfo.cs", "r") as file:
    for line in file:
        match = versionRegex.search(line)

        if (match is not None):
            version = match.group(0)
            break


print("Packing version %s..." % version)

ziprelease(True)

with open('.env') as f:
    env = [line.rstrip('\n') for line in f]

if (not versionRegex.match(version)):
    print("Invalid version format!")
    exit()

print("Connecting...")

filename = "Release.zip"
ftp = ftplib.FTP(env[0])
ftp.login(env[1], env[2])

print("Downloading update manifest...")
with open("updates.man", "wb+") as file:
    ftp.retrbinary("RETR updates.man", file.write)

appendToFile = True

with open("updates.man", "r") as file:
    for line in file:
        if (line.startswith(version)):
            force = len(sys.argv) > 0 and "-force" in sys.argv
            appendToFile = False

            if (not force):
                print("Version already uploaded!")
                file.close()
                clean()
                exit()

if (appendToFile):
    with open("updates.man", "a+") as file:
        file = open("updates.man", "a+")
        file.write("%s ./%s.zip" % (version, version))
        file.close()

    print("Uploading update manifest...")
    with open("updates.man", "rb") as file:
        ftp.storbinary("STOR updates.man", file)

print("Uploading update archive...")
with open("Release.zip", "rb") as file:
    ftp.storbinary("STOR %s.zip" % version, file)

ftp.close()

print("Done!")
clean()
