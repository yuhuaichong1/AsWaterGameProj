#!/usr/bin/env python3
"""将 ConfTotal.json 拆分为 Levels/Split/level_N.json"""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/WaterGame/Resources/Levels/ConfTotal.json"
OUT = ROOT / "Assets/WaterGame/Resources/Levels/Split"


def find_matching(text: str, start: int) -> int:
    depth = 0
    for i in range(start, len(text)):
        if text[i] == "[":
            depth += 1
        elif text[i] == "]":
            depth -= 1
            if depth == 0:
                return i
    return -1


def parse_cup(cup_json: str) -> dict:
    pos = re.search(r'"x"\s*:\s*(-?[\d.]+)\s*,\s*"y"\s*:\s*(-?[\d.]+)', cup_json)
    x = float(pos.group(1)) if pos else 0.0
    y = float(pos.group(2)) if pos else 0.0
    colors_m = re.search(r"\[\s*((?:\d+\s*,\s*)*\d+)?\s*\]", cup_json)
    colors = []
    if colors_m:
        inner = colors_m.group(0).strip("[] ")
        if inner:
            colors = [int(p.strip()) for p in inner.split(",") if p.strip()]
    tail = re.search(
        r"\]\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:\s*,\s*(\d+))?\s*\]",
        cup_json,
    )
    wh = v = lk = lc = ln = nl = 0
    if tail:
        wh, v, lk, lc, ln = map(int, tail.groups()[:5])
        if tail.group(6):
            nl = int(tail.group(6))
    return {
        "x": x,
        "y": y,
        "colors": colors,
        "whNums": wh,
        "isVideo": v,
        "isLock": lk,
        "lockColor": lc,
        "lockNums": ln,
        "isNull": nl,
    }


def parse_level_array(level_json: str) -> list:
    cups = []
    depth = 0
    cup_start = -1
    for i, c in enumerate(level_json):
        if c == "[":
            if depth == 1:
                cup_start = i
            depth += 1
        elif c == "]":
            depth -= 1
            if depth == 1 and cup_start >= 0:
                cups.append(parse_cup(level_json[cup_start : i + 1]))
                cup_start = -1
    return cups


def main() -> None:
    text = SRC.read_text(encoding="utf-8")
    OUT.mkdir(parents=True, exist_ok=True)
    pattern = re.compile(r'"(level_\d+)"\s*:\s*\[')
    count = 0
    for m in pattern.finditer(text):
        key = m.group(1)
        n = int(key.split("_")[1])
        start = m.end() - 1
        end = find_matching(text, start)
        if end < 0:
            continue
        cups = parse_level_array(text[start : end + 1])
        doc = {"level": n, "cups": cups}
        out_path = OUT / f"level_{n}.json"
        out_path.write_text(json.dumps(doc, ensure_ascii=False, indent=2), encoding="utf-8")
        count += 1
    print(f"exported {count} levels -> {OUT}")


if __name__ == "__main__":
    main()
