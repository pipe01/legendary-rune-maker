#!/usr/bin/env python
from __future__ import with_statement
from contextlib import closing
from zipfile import ZipFile, ZIP_DEFLATED
import os

def zipdir(basedir, archivename):
    assert os.path.isdir(basedir)
    with closing(ZipFile(archivename, "w", ZIP_DEFLATED)) as z:
        for root, dirs, files in os.walk(basedir):
            #NOTE: ignore empty directories
            for fn in files:
                absfn = os.path.join(root, fn)
                zfn = absfn[len(basedir)+len(os.sep):] #XXX: relative path

                if (zfn.startswith("cache") or zfn.endswith(".json") or zfn.endswith(".xml") or zfn.endswith(".pdb")):
                    continue

                print(zfn)

                z.write(absfn, zfn)

if __name__ == '__main__':
    import sys
    basedir = "./Legendary Rune Maker/bin/Release"
    archivename = "Legendary Rune Maker.zip"
    zipdir(basedir, archivename)