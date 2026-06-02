#!/usr/bin/env python3
import shutil
from pathlib import Path

SRC = Path(r"d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\ImportedFromCocos\UI")
DST = Path(r"d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\Resources\UI\Popups")

def main():
    DST.mkdir(parents=True, exist_ok=True)
    for prefab in SRC.glob("*.prefab"):
        dst = DST / prefab.name
        shutil.copy2(prefab, dst)
        print("OK", prefab.name)

if __name__ == "__main__":
    main()
