#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import shutil
from pathlib import Path

COCOS = Path(r"d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets")
UNITY = Path(r"d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\Resources\Sprites")

COPIES = [
    (COCOS / "Res/bj_2.png", UNITY / "Home/bg_home.png"),
    (COCOS / "Res/UI/btn_3.png", UNITY / "UI/btn_main.png"),
    (COCOS / "Res/UI/btn_7.png", UNITY / "UI/btn_small.png"),
    (COCOS / "Res/UI/btn_1.png", UNITY / "UI/btn_icon.png"),
    (COCOS / "Res/UI/frame_3.png", UNITY / "UI/frame_prop.png"),
    (COCOS / "Res/UI/img_003.png", UNITY / "UI/panel_bg.png"),
]

def main():
    (UNITY / "UI").mkdir(parents=True, exist_ok=True)
    for src, dst in COPIES:
        if not src.exists():
            print("MISSING", src)
            continue
        shutil.copy2(src, dst)
        print("OK", dst.name)

if __name__ == "__main__":
    main()
