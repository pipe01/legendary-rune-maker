#!/usr/bin/env python
import ftplib
import re
import os
from __future__ import with_statement
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
    os.remove("updates.man")
    os.remove("Release.zip")


versionRegex = re.compile("\\d\\.\\d\\.\\d")

with open("./Legendary Rune Maker/Properties/AssemblyInfo.cs", "r") as file:
    for line in file:
        match = versionRegex.search(line)

        if (match is not None):
            version = match.group(0)
            break


print("Packing version %s..." % version)

pack_release.ziprelease(True)

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

updateManifest = open("updates.man", "wb+")

ftp.retrbinary("RETR updates.man", updateManifest.write)

updateManifest.close()

with open("updates.man", "r") as file:
    for line in file:
        if (line.startswith(version)):
            print("Version already uploaded!")
            file.close()
            clean()
            exit()

updateManifest = open("updates.man", "a+")

updateManifest.write(version + " ./" + version + ".zip\n")

updateManifest.close()
updateManifest = open("updates.man", "rb")

print("Uploading update manifest...")
ftp.storbinary("STOR updates.man", updateManifest)

updateManifest.close()

myfile = open(filename, 'rb')

print("Uploading update archive...")
ftp.storbinary("STOR %s.zip" % version, myfile)

myfile.close()

ftp.close()

print("Done!")
clean()
