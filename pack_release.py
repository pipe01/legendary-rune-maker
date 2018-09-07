#!/usr/bin/env python
from __future__ import with_statement
import os
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


code = os.system("msbuild /p:Configuration=Release /v:m")

if (code != 0):
    print("Build failed!")
    input("Press Enter to continue...")
    exit()

print("Packing...")
ziprelease(True)
