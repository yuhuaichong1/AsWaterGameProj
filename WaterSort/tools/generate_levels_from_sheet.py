# -*- coding: utf-8 -*-
"""按 LevelDesignV1.json 生成 level_1~60.json；瓶子吸附局内预览网格。"""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

MAX_CAPACITY = 4
ROOT = Path(__file__).resolve().parents[1]
DESIGN_JSON = ROOT / "Assets/WaterGame/Editor/LevelEditor/LevelDesignV1.json"
EXCEL_PATH = Path(r"d:\小游戏规划\WaterSort\关卡设计\关卡设计.xls")
OUT_DIR = ROOT / "Assets/WaterGame/Resources/Levels/Split"

DESIGN_W, DESIGN_H = 1200, 2132
INSET_TOP, INSET_BOTTOM = 800, 240
CELL_W = 91
USER_ROWS = 5

NULL_SLOTS = [(280, -540), (220, -540)]


def export_design_from_excel():
    if not EXCEL_PATH.exists():
        return
    import xlrd
    sh = xlrd.open_workbook(str(EXCEL_PATH)).sheet_by_name("优化版v1")
    rows = []
    for r in range(1, sh.nrows):
        row = [sh.cell_value(r, c) for c in range(10)]
        rows.append({
            "level": int(row[0]),
            "regular": int(row[1]),
            "empty": int(row[2]),
            "lockCup": int(row[3]),
            "lockLayers": int(row[4]),
            "ad": int(row[5]),
            "slot": int(row[6]),
            "colors": int(row[7]),
            "layers": int(row[8]),
        })
    DESIGN_JSON.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")


def play_area_rect():
    half_w, half_h = DESIGN_W * 0.5, DESIGN_H * 0.5
    return {
        "xmin": -half_w,
        "xmax": half_w,
        "ymin": -half_h + INSET_BOTTOM,
        "ymax": half_h - INSET_TOP,
    }


def cup_area_rect():
    return {"xmin": -375, "xmax": 375, "ymin": -667, "ymax": 667}


def play_to_cup(px, py):
    play = play_area_rect()
    cup = cup_area_rect()
    tx = (px - play["xmin"]) / (play["xmax"] - play["xmin"])
    ty = (py - play["ymin"]) / (play["ymax"] - play["ymin"])
    cx = cup["xmin"] + tx * (cup["xmax"] - cup["xmin"])
    cy = cup["ymin"] + ty * (cup["ymax"] - cup["ymin"])
    return cx, cy


def clamp_cup(x, y, cell_h=245):
    cup = cup_area_rect()
    half_w = CELL_W * 0.5
    x = max(cup["xmin"] + half_w, min(cup["xmax"] - half_w, x))
    y = max(cup["ymin"], min(cup["ymax"] - cell_h, y))
    return x, y


def plan_rows(n):
    if n <= 6:
        return [(3, n)]
    if n <= 12:
        top = n // 2
        return [(2, top), (4, n - top)]
    r1 = n // 3
    r3 = n // 3
    return [(1, r1), (3, r3), (5, n - r1 - r3)]


def centered_columns(count, total_cols):
    span = 2 * count - 1 if count > 0 else 0
    start = max(0, (total_cols - span) // 2)
    return [start + i * 2 for i in range(count)]


def build_grid_positions(bottle_count):
    play = play_area_rect()
    play_w = play["xmax"] - play["xmin"]
    cell_h = (play["ymax"] - play["ymin"]) / USER_ROWS
    total_cols = max(1, int(play_w // CELL_W))
    points = []
    for user_row, count in plan_rows(bottle_count):
        index_from_bottom = USER_ROWS - user_row
        y = play["ymin"] + index_from_bottom * cell_h
        for col in centered_columns(count, total_cols):
            x = play["xmin"] + (col + 0.5) * CELL_W
            points.append(clamp_cup(*play_to_cup(x, y), cell_h))
    return points


def is_uniform_full(layers):
    return len(layers) == MAX_CAPACITY and len(set(layers)) == 1 and layers[0] >= 1


def distribute_even(total, cup_count):
    base = total // cup_count
    rem = total % cup_count
    return [base + (1 if i < rem else 0) for i in range(cup_count)]


def build_interleaved_stream(pool, rng):
    buckets = {}
    for c in pool:
        buckets.setdefault(c, []).append(c)
    colors = list(buckets.keys())
    rng.shuffle(colors)
    stream = []
    guard = 0
    while len(stream) < len(pool) and guard < len(pool) * 8:
        guard += 1
        progressed = False
        rng.shuffle(colors)
        for color in colors:
            if buckets[color]:
                stream.append(buckets[color].pop(0))
                progressed = True
        if not progressed:
            break
    return stream


def try_deal_even(virtuals, pool, rng):
    stream = build_interleaved_stream(pool[:], rng)
    if len(stream) != len(pool):
        return False
    order = list(range(len(virtuals)))
    rng.shuffle(order)
    filled = [0] * len(virtuals)
    cursor = 0
    for color in stream:
        placed = False
        for t in range(len(virtuals)):
            ci = order[(cursor + t) % len(virtuals)]
            if filled[ci] >= virtuals[ci]["target"]:
                continue
            virtuals[ci]["layers"].append(color)
            filled[ci] += 1
            cursor = (cursor + t + 1) % len(virtuals)
            placed = True
            break
        if not placed:
            return False
    return all(filled[i] == virtuals[i]["target"] for i in range(len(virtuals)))


def try_pour(src, dst):
    if not src["layers"] or len(dst["layers"]) >= MAX_CAPACITY:
        return False
    color = src["layers"][-1]
    if dst["layers"] and dst["layers"][-1] != color:
        return False
    run = 1
    for i in range(len(src["layers"]) - 2, -1, -1):
        if src["layers"][i] != color:
            break
        run += 1
    move = min(run, MAX_CAPACITY - len(dst["layers"]))
    if move <= 0:
        return False
    for _ in range(move):
        dst["layers"].append(src["layers"].pop())
    return True


def scramble(virtuals, steps, rng):
    for _ in range(steps):
        a, b = rng.randrange(len(virtuals)), rng.randrange(len(virtuals))
        if a != b:
            try_pour(virtuals[a], virtuals[b])


def valid_regular(layers):
    if not layers:
        return False
    if is_uniform_full(layers):
        return False
    if len(layers) == MAX_CAPACITY and len(set(layers)) < 2:
        return False
    return True


def try_refresh_water(cups, color_count, total_layers, lock_layer_counts, difficulty, rng):
    lock_cups = [c for c in cups if c["kind"] == "lock"]
    regular_cups = [c for c in cups if c["kind"] == "regular"]

    pool = []
    for c in range(1, color_count + 1):
        pool.extend([c] * 4)

    for cup, layers in zip(lock_cups, lock_layer_counts):
        cup["colors"] = []
        for _ in range(layers):
            if not pool:
                return False
            idx = rng.randrange(len(pool))
            cup["colors"].append(pool.pop(idx))
        cup["whNums"] = 0

    regular_layers = total_layers - sum(lock_layer_counts)
    if regular_layers < 0:
        return False
    if regular_layers == 0:
        for c in regular_cups:
            c["colors"] = []
        return True
    if regular_layers > len(regular_cups) * MAX_CAPACITY:
        return False

    layer_counts = distribute_even(regular_layers, len(regular_cups))
    virtuals = [{"target": t, "layers": []} for t in layer_counts]
    if not try_deal_even(virtuals, pool, rng):
        return False
    steps = rng.randint(10, 30) if difficulty <= 1 else rng.randint(20, 50)
    scramble(virtuals, steps, rng)
    for v, cup in zip(virtuals, regular_cups):
        if not valid_regular(v["layers"]):
            return False
        cup["colors"] = v["layers"][:]
        cup["whNums"] = 0
    return True


def make_cup(x, y, kind):
    return {
        "x": x, "y": y, "colors": [], "whNums": 0,
        "isVideo": 1 if kind == "ad" else 0,
        "isLock": 1 if kind == "lock" else 0,
        "lockColor": 0, "lockNums": 0,
        "isNull": 1 if kind == "null" else 0,
        "isEmptyCup": 1 if kind == "empty" else 0,
        "kind": kind,
    }


def build_level(spec, rng):
    grid_n = spec["regular"] + spec["empty"] + spec["lockCup"] + spec["ad"]
    positions = build_grid_positions(grid_n)
    if len(positions) < grid_n:
        return None

    cups = []
    idx = 0
    for _ in range(spec["lockCup"]):
        c = make_cup(*positions[idx], "lock")
        c["lockNums"] = 1 if spec["level"] <= 40 else 2
        cups.append(c)
        idx += 1
    for _ in range(spec["regular"]):
        cups.append(make_cup(*positions[idx], "regular"))
        idx += 1
    for _ in range(spec["empty"]):
        cups.append(make_cup(*positions[idx], "empty"))
        idx += 1
    for _ in range(spec["ad"]):
        cups.append(make_cup(*positions[idx], "ad"))
        idx += 1
    for s in range(spec["slot"]):
        cups.append(make_cup(*clamp_cup(*NULL_SLOTS[s]), "null"))

    lock_layers = [spec["lockLayers"]] * spec["lockCup"] if spec["lockCup"] else []
    diff = 0 if spec["level"] <= 2 else (1 if spec["level"] <= 40 else 2)
    if not try_refresh_water(cups, spec["colors"], spec["layers"], lock_layers, diff, rng):
        return None

    for c in cups:
        c.pop("kind", None)
    return cups


def validate(spec, cups):
    regular = sum(1 for c in cups if not any([c["isNull"], c["isVideo"], c["isLock"], c["isEmptyCup"]]))
    if regular != spec["regular"]:
        return False, "regular"
    for c in cups:
        if c["isLock"] and len(c["colors"]) != spec["lockLayers"]:
            return False, "lock"
        if not c["isLock"] and not c["isEmptyCup"] and not c["isNull"] and not c["isVideo"]:
            if len(c["colors"]) <= 0:
                return False, "empty regular"
            if is_uniform_full(c["colors"]):
                return False, "full"
        if c["isEmptyCup"] and c["colors"]:
            return False, "empty cup water"
    return True, ""


def generate_level(spec):
    for attempt in range(512):
        rng = random.Random(spec["level"] * 10007 + 17 + attempt * 131)
        cups = build_level(spec, rng)
        if cups is None:
            continue
        ok, _ = validate(spec, cups)
        if ok:
            return cups
    return None


def main():
    export_design_from_excel()
    specs = json.loads(DESIGN_JSON.read_text(encoding="utf-8"))
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    ok, fail = 0, []
    for spec in specs:
        lv = spec["level"]
        cups = generate_level(spec)
        if cups is None:
            fail.append(lv)
            continue
        doc = {"level": lv, "cups": cups}
        out = OUT_DIR / f"level_{lv}.json"
        out.write_text(json.dumps(doc, ensure_ascii=False, indent=4), encoding="utf-8")
        ok += 1
        print(f"level_{lv}.json OK")
    print(f"done: {ok} ok, {len(fail)} fail {fail}")


if __name__ == "__main__":
    main()
