#!/usr/bin/env python3
import shutil
from pathlib import Path

COCOS = Path(r"d:\BaiduNetdiskDownload\水排序\KaiGeDaNaoMen2.4.13js\assets")
UNITY = Path(r"d:\BaiduNetdiskDownload\水排序\WaterSort\Assets\Resources\Sprites")

COPIES = [
    (COCOS / "Res/game_bg_2.png", UNITY / "Game/game_bg_2.png"),
    (COCOS / "Res/bj_3.png", UNITY / "Game/bj_3.png"),
    (COCOS / "Res/GameRes/new/icon_3.png", UNITY / "UI/icon_add_bottle.png"),
    (COCOS / "Res/GameRes/new/icon_6.png", UNITY / "UI/icon_undo.png"),
    (COCOS / "Res/GameRes/new/icon_7.png", UNITY / "UI/icon_shuffle.png"),
    (COCOS / "Res/UI/btn_1.png", UNITY / "UI/btn_prop.png"),
]
POCKET_COPIES = [
    (COCOS / "Res/GameRes/pocket", UNITY / "Pocket"),
]

def copy_dir(src_dir, dst_dir):
    if not src_dir.exists():
        print("MISSING dir", src_dir)
        return
    dst_dir.mkdir(parents=True, exist_ok=True)
    for png in src_dir.glob("*.png"):
        shutil.copy2(png, dst_dir / png.name)
        print("OK pocket", png.name)

def main():
    (UNITY / "Game").mkdir(parents=True, exist_ok=True)
    for src, dst in COPIES:
        if not src.exists():
            print("MISSING", src)
            continue
        shutil.copy2(src, dst)
        print("OK", dst.name)
    for src_dir, dst_dir in POCKET_COPIES:
        copy_dir(src_dir, dst_dir)

if __name__ == "__main__":
    main()
