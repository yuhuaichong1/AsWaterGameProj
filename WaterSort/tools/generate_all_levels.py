#!/usr/bin/env python3
"""批量生成 Levels/Split/level_1.json ~ level_161.json（与 LevelBatchGenerator 规则一致）。"""

from __future__ import annotations

import json
import math
import random
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import List, Optional, Tuple

from level_water_solver import try_find_min_steps

MAX_LEVEL = 161
MAX_CAPACITY = 4
MAX_ATTEMPTS = 256
OUTPUT_DIR = Path(__file__).resolve().parents[1] / "Assets/WaterGame/Resources/Levels/Split"

MAIN_GRID = [
    (0, 40), (-110, 40), (110, 40),
    (-220, -50), (220, -50),
    (0, -140),
    (-110, -230), (110, -230),
    (-220, -320), (220, -320),
    (0, -410),
    (-110, -500), (110, -500),
]
AD_SLOT = (-280, -540)
NULL_SLOT = (280, -540)

# CupMgr clamp (91×242)
BOTTLE_W, BOTTLE_H = 91, 242
CUP_X_MIN, CUP_X_MAX = -375 + BOTTLE_W / 2, 375 - BOTTLE_W / 2
CUP_Y_MIN, CUP_Y_MAX = -667, 667 - BOTTLE_H


class Difficulty:
    SUPER_EASY = 0
    EASY = 1
    MEDIUM = 2
    HARD = 3
    VERY_HARD = 4


@dataclass
class LevelGenSpec:
    level: int
    color_count: int
    regular_cup_count: int
    lock_cup_count: int
    empty_bottle_count: int
    has_ad: bool
    has_null: bool
    difficulty: int
    lock_nums: int
    use_colored_lock: bool
    lock_layer_counts: List[int]


def compute_color_count_early(level: int) -> int:
    if level <= 2:
        return 2
    if level <= 40:
        return 2
    if level <= 70:
        return 3
    if level <= 85:
        return 4
    if level <= 95:
        return 5
    return 6


def compute_lock_cup_count(level: int) -> int:
    if level < 3:
        return 0
    if level <= 70:
        return 1
    if level <= 99:
        return 2 if level % 2 == 0 else 1
    if level < 40:
        return 2 if level % 3 == 0 else 1
    return 2 if level % 2 == 0 else 1


def build_lock_layer_counts(level: int, lock_cup_count: int) -> List[int]:
    if lock_cup_count <= 0:
        return []
    if 3 <= level <= 5:
        per = 2
    elif 6 <= level <= 10:
        per = 3
    elif level <= 40:
        per = 3
    else:
        per = MAX_CAPACITY
    return [per] * lock_cup_count


def build_spec(level: int) -> LevelGenSpec:
    if level <= 99:
        color_count = compute_color_count_early(level)
        difficulty = Difficulty.SUPER_EASY if level <= 2 else Difficulty.EASY
    else:
        t = (level - 1) / (MAX_LEVEL - 1)
        color_count = max(2, min(6, 2 + int(t * 4.01)))
        difficulty = Difficulty.EASY if level <= 70 else Difficulty.MEDIUM

    empty_bottle_count = 1 if level <= 20 else (2 if level % 8 == 0 else 1)
    empty_bottle_count = max(1, min(2, empty_bottle_count))
    has_ad = level >= 3
    has_null = level >= 3
    lock_cup_count = compute_lock_cup_count(level)
    lock_nums = 1 if level <= 10 else (1 if level <= 40 else 2)
    use_colored_lock = level >= 100
    lock_layer_counts = build_lock_layer_counts(level, lock_cup_count)

    total_layers = color_count * 4
    lock_layers_sum = sum(lock_layer_counts)
    regular_layers = total_layers - lock_layers_sum
    if regular_layers <= 0:
        regular_cup_count = 0
    else:
        min_regular = math.ceil(regular_layers / 4)
        extra_regular = 1 if level <= 2 else 2 + (level - 3) // 2
        regular_cup_count = min(min_regular + extra_regular, 9)
        if regular_cup_count < min_regular:
            regular_cup_count = min_regular

    return LevelGenSpec(
        level=level,
        color_count=color_count,
        regular_cup_count=regular_cup_count,
        lock_cup_count=lock_cup_count,
        empty_bottle_count=empty_bottle_count,
        has_ad=has_ad,
        has_null=has_null,
        difficulty=difficulty,
        lock_nums=lock_nums,
        use_colored_lock=use_colored_lock,
        lock_layer_counts=lock_layer_counts,
    )


def clamp_pos(x: float, y: float) -> Tuple[float, float]:
    return (
        max(CUP_X_MIN, min(CUP_X_MAX, x)),
        max(CUP_Y_MIN, min(CUP_Y_MAX, y)),
    )


def shuffle(lst: list, rng: random.Random) -> None:
    for i in range(len(lst) - 1, 0, -1):
        j = rng.randint(0, i)
        lst[i], lst[j] = lst[j], lst[i]


def perturb_spec(base: LevelGenSpec, attempt: int, rng: random.Random) -> LevelGenSpec:
    s = LevelGenSpec(**base.__dict__)
    s.lock_layer_counts = list(base.lock_layer_counts)
    if attempt % 3 == 1 and s.regular_cup_count > 2:
        s.regular_cup_count -= 1
    elif attempt % 3 == 2:
        s.regular_cup_count += 1
    if attempt > 24 and s.lock_cup_count > 0 and rng.random() < 0.3:
        s.lock_cup_count -= 1
    return s


def pick_grid_positions(count: int, level: int) -> List[Tuple[float, float]]:
    rng = random.Random(level * 7919 + 3)
    indices = list(range(len(MAIN_GRID)))
    shuffle(indices, rng)
    picked: List[Tuple[float, float]] = []
    min_dist = 95.0
    for idx in indices:
        if len(picked) >= count:
            break
        p = clamp_pos(*MAIN_GRID[idx])
        if all(math.hypot(p[0] - q[0], p[1] - q[1]) >= min_dist for q in picked):
            picked.append(p)
    if len(picked) < count:
        return []
    picked.sort(key=lambda p: (-p[1], p[0]))
    return picked


def cup_entry(x, y, colors=None, wh=0, is_video=0, is_lock=0, lock_color=0, lock_nums=0,
              is_null=0, is_empty=0) -> dict:
    return {
        "x": float(x),
        "y": float(y),
        "colors": list(colors or []),
        "whNums": wh,
        "isVideo": is_video,
        "isLock": is_lock,
        "lockColor": lock_color,
        "lockNums": lock_nums,
        "isNull": is_null,
        "isEmptyCup": is_empty,
    }


def build_tutorial_level1() -> dict:
    cups = [
        cup_entry(-110, -40, [1, 1, 1, 2]),
        cup_entry(110, -40, [2, 2, 2, 1]),
        cup_entry(0, -200, is_empty=1),
    ]
    return {"level": 1, "cups": cups}


# --- water randomizer (port of LevelWaterRandomizer) ---

def build_color_pool(color_count: int) -> List[int]:
    pool = []
    for c in range(1, color_count + 1):
        pool.extend([c] * 4)
    return pool


def count_in_pool(pool: List[int], color: int) -> int:
    return sum(1 for c in pool if c == color)


def remove_from_pool(pool: List[int], color: int, count: int) -> None:
    for _ in range(count):
        for i, c in enumerate(pool):
            if c == color:
                pool.pop(i)
                break


def fill_lock_cup(cup: dict, pool: List[int], rng: random.Random, layer_count: int) -> bool:
    layer_count = max(1, min(MAX_CAPACITY, layer_count))
    cup["colors"] = []
    lc = cup["lockColor"]
    if lc > 0:
        if count_in_pool(pool, lc) < layer_count:
            return False
        remove_from_pool(pool, lc, layer_count)
        cup["colors"] = [lc] * layer_count
    else:
        if len(pool) < layer_count:
            return False
        for _ in range(layer_count):
            idx = rng.randrange(len(pool))
            cup["colors"].append(pool.pop(idx))
    cup["whNums"] = 0
    return len(cup["colors"]) == layer_count


def get_empty_cup_target(cup_count: int, total_layers: int, difficulty: int, rng: random.Random) -> int:
    if cup_count <= 1:
        return 0
    min_fill = max(1, math.ceil(total_layers / MAX_CAPACITY))
    max_empty = max(0, cup_count - min_fill)
    if max_empty == 0:
        return 0
    ratio = {Difficulty.SUPER_EASY: 0.42, Difficulty.EASY: 0.32, Difficulty.MEDIUM: 0.22}.get(difficulty, 0.12)
    target = round(cup_count * ratio)
    target = max(0, min(max_empty, target))
    if target == 0 and max_empty > 0 and difficulty <= Difficulty.EASY:
        target = 1
    return target


def distribute_layer_counts(layer_counts: List[int], fill_indices: List[int], total_layers: int,
                            difficulty: int, rng: random.Random) -> bool:
    if total_layers == 0:
        return True
    n = len(fill_indices)
    if n == 0:
        return False
    if total_layers < n or total_layers > n * MAX_CAPACITY:
        return False
    for idx in fill_indices:
        layer_counts[idx] = 1
    remaining = total_layers - n
    prefer_high = difficulty >= Difficulty.MEDIUM
    guard = 0
    while remaining > 0 and guard < 512:
        guard += 1
        cup = fill_indices[rng.randrange(n)]
        if layer_counts[cup] >= MAX_CAPACITY:
            continue
        add = 1
        if prefer_high and layer_counts[cup] < MAX_CAPACITY - 1 and rng.random() < 0.35:
            add = min(remaining, 2, MAX_CAPACITY - layer_counts[cup])
        layer_counts[cup] += add
        remaining -= add
    return remaining == 0


class VirtualBottle:
    def __init__(self, target: int):
        self.target = target
        self.layers: List[int] = []


def try_pour(src: VirtualBottle, dst: VirtualBottle) -> bool:
    if not src.layers or len(dst.layers) >= MAX_CAPACITY:
        return False
    color = src.layers[-1]
    if dst.layers and dst.layers[-1] != color:
        return False
    run = 1
    for i in range(len(src.layers) - 2, -1, -1):
        if src.layers[i] != color:
            break
        run += 1
    space = MAX_CAPACITY - len(dst.layers)
    move = min(run, space)
    if move <= 0:
        return False
    for _ in range(move):
        dst.layers.append(src.layers.pop())
    return True


def count_distinct(layers: List[int]) -> int:
    return len(set(layers))


def get_shuffle_steps(difficulty: int, rng: random.Random) -> int:
    ranges = {
        Difficulty.SUPER_EASY: (5, 16),
        Difficulty.EASY: (10, 31),
        Difficulty.MEDIUM: (20, 51),
        Difficulty.HARD: (40, 71),
        Difficulty.VERY_HARD: (70, 151),
    }
    lo, hi = ranges.get(difficulty, (70, 151))
    return rng.randrange(lo, hi)


def pick_wh_nums(difficulty: int, layer_count: int, rng: random.Random) -> int:
    if layer_count <= 1:
        return 0
    if difficulty <= Difficulty.EASY:
        return 0
    if difficulty == Difficulty.MEDIUM:
        return rng.randrange(1, min(2, layer_count) + 1) if rng.random() < 0.2 else 0
    if difficulty == Difficulty.HARD:
        return rng.randrange(1, min(3, layer_count) + 1) if rng.random() < 0.4 else 0
    return rng.randrange(1, min(3, layer_count) + 1) if rng.random() < 0.5 else 0


def fill_regular_cups(regular_cups: List[dict], regular_layers: int, pool: List[int],
                      difficulty: int, rng: random.Random) -> bool:
    cup_count = len(regular_cups)
    min_fill = max(1, math.ceil(regular_layers / MAX_CAPACITY))
    empty_target = get_empty_cup_target(cup_count, regular_layers, difficulty, rng)
    empty_target = min(empty_target, max(0, cup_count - min_fill))
    fill_cup_count = cup_count - empty_target
    if regular_layers > fill_cup_count * MAX_CAPACITY:
        return False
    if regular_layers > 0 and fill_cup_count > 0 and regular_layers < fill_cup_count:
        return False
    if regular_layers == 0 and fill_cup_count > 0:
        fill_cup_count = 0

    layer_counts = [0] * cup_count
    fill_indices = list(range(cup_count))
    shuffle(fill_indices, rng)
    fill_indices = fill_indices[:max(0, fill_cup_count)]

    if not distribute_layer_counts(layer_counts, fill_indices, regular_layers, difficulty, rng):
        return False

    virtuals = [VirtualBottle(layer_counts[i]) for i in range(cup_count)]
    shuffle(pool, rng)
    idx = 0
    for v in virtuals:
        for _ in range(v.target):
            if idx >= len(pool):
                return False
            v.layers.append(pool[idx])
            idx += 1
    if idx != len(pool):
        return False

    steps = get_shuffle_steps(difficulty, rng)
    max_colors = {Difficulty.SUPER_EASY: 2, Difficulty.EASY: 3, Difficulty.MEDIUM: 4}.get(difficulty, MAX_CAPACITY)
    for _ in range(steps):
        a, b = rng.randrange(cup_count), rng.randrange(cup_count)
        if a != b:
            try_pour(virtuals[a], virtuals[b])

    for i, cup in enumerate(regular_cups):
        cup["colors"] = list(virtuals[i].layers)
        cup["whNums"] = pick_wh_nums(difficulty, len(cup["colors"]), rng)
    return True


def try_refresh(cups: List[dict], color_count: int, total_layers: int, difficulty: int,
                rng: random.Random, lock_layer_counts: Optional[List[int]] = None) -> bool:
    lock_cups = [c for c in cups if c.get("isLock")]
    regular_cups = [c for c in cups if not c.get("isNull") and not c.get("isVideo")
                    and not c.get("isLock") and not c.get("isEmptyCup")]

    if lock_layer_counts is None or len(lock_layer_counts) != len(lock_cups):
        lock_layer_counts = [MAX_CAPACITY] * len(lock_cups)
    lock_layers = sum(lock_layer_counts)
    if lock_layers > total_layers:
        return False
    regular_layers = total_layers - lock_layers
    if not regular_cups and regular_layers > 0:
        return False
    if regular_cups:
        cap = len(regular_cups) * MAX_CAPACITY
        if regular_layers > cap:
            return False
        if regular_layers > 0 and len(regular_cups) < math.ceil(regular_layers / MAX_CAPACITY):
            return False

    pool = build_color_pool(color_count)
    shuffle(pool, rng)
    for i, cup in enumerate(lock_cups):
        if not fill_lock_cup(cup, pool, rng, lock_layer_counts[i]):
            return False
    if not regular_cups:
        return len(pool) == 0
    return fill_regular_cups(regular_cups, regular_layers, pool, difficulty, rng)


def participates(c: dict) -> bool:
    return not c.get("isNull") and not c.get("isVideo") and not c.get("isEmptyCup")


def is_uniform_full(layers: List[int]) -> bool:
    return len(layers) == MAX_CAPACITY and layers and all(x == layers[0] for x in layers)


def recommend_difficulty(participating: int, color_count: int, total_layers: int, lock_count: int) -> int:
    if participating <= 0 or total_layers <= 0:
        return Difficulty.SUPER_EASY
    regular = max(1, participating - lock_count)
    density = total_layers / (participating * MAX_CAPACITY)
    color_pressure = color_count / regular
    if color_count <= 3 and total_layers <= 16 and density <= 0.55:
        return Difficulty.SUPER_EASY
    if color_count <= 4 and density <= 0.65 and color_pressure <= 0.9:
        return Difficulty.EASY
    if color_count <= 6 and density <= 0.8:
        return Difficulty.MEDIUM
    if color_count <= 7 and density <= 0.92:
        return Difficulty.HARD
    return Difficulty.VERY_HARD


def quick_lock_deadlock(cups: List[dict]) -> bool:
    """锁瓶未解锁时，外部是否至少能先装袋一次（必要条件）。"""
    lock_cups = [c for c in cups if c.get("isLock") and (c.get("lockNums") or 1) > 0]
    if not lock_cups:
        return False

    def count_in(source: List[dict]) -> dict[int, int]:
        cnt: dict[int, int] = {}
        for c in source:
            for col in c.get("colors") or []:
                cnt[col] = cnt.get(col, 0) + 1
        return cnt

    outside = count_in([
        c for c in cups
        if not c.get("isNull") and not c.get("isVideo") and not c.get("isEmptyCup") and not c.get("isLock")
    ])

    for lock in lock_cups:
        need = lock.get("lockColor") or 0
        if need == 0:
            if not any(n >= MAX_CAPACITY for n in outside.values()):
                return True
        elif outside.get(need, 0) < MAX_CAPACITY:
            return True
    return False


def validate_layout(cups: List[dict]) -> Optional[str]:
    min_dist = 82
    top_avoid = 130
    bottom_avoid = -560
    visible = [c for c in cups if not c.get("isNull")]
    for c in visible:
        if not c.get("isVideo") and c["y"] > top_avoid:
            return "布局过高"
    for c in visible:
        if c["y"] < bottom_avoid and not c.get("isVideo") and not c.get("isEmptyCup"):
            return "布局过低"
    for i, a in enumerate(visible):
        for b in visible[i + 1:]:
            if a.get("isVideo") or b.get("isVideo"):
                continue
            dx, dy = a["x"] - b["x"], a["y"] - b["y"]
            if math.hypot(dx, dy) < min_dist:
                return f"间距过近 {math.hypot(dx, dy):.0f}"
    return None


def validate_level(level: int, cups: List[dict]) -> Optional[str]:
    participating = lock = regular = empty = video = null = 0
    color_counts: dict[int, int] = {}
    total_layers = 0

    for i, c in enumerate(cups):
        if c.get("isNull"):
            null += 1
            continue
        if c.get("isEmptyCup"):
            empty += 1
            continue
        if c.get("isVideo"):
            video += 1
            continue
        participating += 1
        layers = c["colors"]
        lc = len(layers)
        total_layers += lc
        if c.get("isLock"):
            lock += 1
            if lc < 1 or lc > MAX_CAPACITY:
                return f"#{i} 锁瓶层数 {lc} 无效"
            if 3 <= level <= 5 and lc != 2:
                return f"#{i} 第{level}关锁瓶应为2层"
            if 6 <= level <= 10 and lc != 3:
                return f"#{i} 第{level}关锁瓶应为3层"
            if (3 <= level <= 10 and lc >= MAX_CAPACITY):
                return f"#{i} 前10关锁瓶不应满瓶"
            if 11 <= level <= 40 and lc >= MAX_CAPACITY:
                return f"#{i} 第{level}关锁瓶建议不超过3层（当前{lc}）"
        else:
            regular += 1
            if is_uniform_full(layers):
                return f"#{i} 普通瓶满瓶同色"
        for col in layers:
            color_counts[col] = color_counts.get(col, 0) + 1

    if participating == 0:
        return "无参与瓶"
    if total_layers % 4:
        return f"水层总数 {total_layers} 非 4 倍数"
    for col, n in color_counts.items():
        if n % 4:
            return f"颜色 {col} 出现 {n} 次"

    if empty < 1 or empty > 2:
        return f"空瓶数 {empty} 不在 1~2"
    if level >= 3:
        if video != 1:
            return f"广告瓶 {video} != 1"
        if null != 1:
            return f"空槽 {null} != 1"
    elif video or null:
        return "前两关不应有广告/空槽"

    if level == 1:
        if len(cups) != 3:
            return "第1关须3瓶"
        if len(color_counts) != 2:
            return "第1关须2色"

    rec = recommend_difficulty(participating, len(color_counts), total_layers, lock)
    if level > 2 and rec > Difficulty.MEDIUM:
        return f"难度 {rec} 超中等"
    if level >= MAX_LEVEL - 5 and rec < Difficulty.EASY and level < 100:
        return f"末关难度偏低 {rec}"

    layout_err = validate_layout(cups)
    if layout_err:
        return layout_err

    solve = try_find_min_steps(cups, level, max_depth=250, max_visited=120000)
    if not solve.is_solvable:
        return solve.message or "关卡不可解"

    return None


def build_level(spec: LevelGenSpec, rng: random.Random) -> Optional[List[dict]]:
    slot_count = spec.regular_cup_count + spec.lock_cup_count + spec.empty_bottle_count
    if slot_count > len(MAIN_GRID):
        return None
    positions = pick_grid_positions(slot_count, spec.level)
    idx = 0
    cups: List[dict] = []

    lock_colors = [
        0 if not spec.use_colored_lock else rng.randint(1, spec.color_count)
        for _ in range(spec.lock_cup_count)
    ]
    for i in range(spec.lock_cup_count):
        x, y = positions[idx]
        idx += 1
        cups.append(cup_entry(x, y, is_lock=1, lock_color=lock_colors[i], lock_nums=spec.lock_nums))

    for _ in range(spec.regular_cup_count):
        x, y = positions[idx]
        idx += 1
        cups.append(cup_entry(x, y))

    for _ in range(spec.empty_bottle_count):
        x, y = positions[idx]
        idx += 1
        cups.append(cup_entry(x, y, is_empty=1))

    if spec.has_ad:
        x, y = clamp_pos(*AD_SLOT)
        cups.append(cup_entry(x, y, is_video=1))
    if spec.has_null:
        x, y = clamp_pos(*NULL_SLOT)
        cups.append(cup_entry(x, y, is_null=1))

    total_layers = spec.color_count * 4
    if not try_refresh(cups, spec.color_count, total_layers, spec.difficulty, rng,
                       spec.lock_layer_counts):
        return None
    return cups


def solver_limits(level: int, *, full: bool = False) -> tuple[int, int]:
    if full:
        return 250, 120000
    if level <= 50:
        return 180, 25000
    return 150, 15000


def generate_level(level: int) -> Tuple[Optional[dict], Optional[str]]:
    if level == 1:
        doc = build_tutorial_level1()
        err = validate_level(1, doc["cups"])
        return (doc, err)

    spec = build_spec(level)
    rng = random.Random(level * 10007 + 13)
    for attempt in range(MAX_ATTEMPTS):
        s = spec if attempt == 0 else perturb_spec(spec, attempt, rng)
        try_rng = random.Random(level * 10007 + 13 + attempt * 997)
        cups = build_level(s, try_rng)
        if cups is None:
            continue
        if validate_structure(level, cups) is not None:
            continue
        if quick_lock_deadlock(cups):
            continue
        md, mv = solver_limits(level)
        solve = try_find_min_steps(cups, level, max_depth=md, max_visited=mv)
        if not solve.is_solvable:
            continue
        return ({"level": level, "cups": cups}, None)
    return (None, "多次随机失败（不可解或校验未通过）")


def validate_structure(level: int, cups: List[dict]) -> Optional[str]:
    """结构校验（不含 BFS，供生成循环快速失败）。"""
    participating = lock = empty = video = null = 0
    color_counts: dict[int, int] = {}
    total_layers = 0
    for i, c in enumerate(cups):
        if c.get("isNull"):
            null += 1
            continue
        if c.get("isEmptyCup"):
            empty += 1
            continue
        if c.get("isVideo"):
            video += 1
            continue
        participating += 1
        layers = c["colors"]
        lc = len(layers)
        total_layers += lc
        if c.get("isLock"):
            lock += 1
            if lc < 1 or lc > MAX_CAPACITY:
                return f"锁瓶层数无效"
            if 3 <= level <= 5 and lc != 2:
                return "锁瓶层数"
            if 6 <= level <= 10 and lc != 3:
                return "锁瓶层数"
        else:
            if is_uniform_full(layers):
                return "满瓶同色"
        for col in layers:
            color_counts[col] = color_counts.get(col, 0) + 1
    if total_layers % 4:
        return "水层"
    for n in color_counts.values():
        if n % 4:
            return "颜色倍数"
    if empty < 1 or empty > 2:
        return "空瓶"
    if level >= 3 and (video != 1 or null != 1):
        return "广告空槽"
    layout_err = validate_layout(cups)
    if layout_err:
        return layout_err
    return None


def write_json(path: Path, doc: dict) -> None:
    # 对齐 Unity JsonUtility 风格（字段顺序与 .0 小数）
    lines = ["{"]
    lines.append(f'    "level": {doc["level"]},')
    lines.append('    "cups": [')
    for ci, cup in enumerate(doc["cups"]):
        comma = "," if ci < len(doc["cups"]) - 1 else ""
        lines.append("        {")
        lines.append(f'            "x": {cup["x"]:.1f},')
        lines.append(f'            "y": {cup["y"]:.1f},')
        colors = cup["colors"]
        if colors:
            cl = ",\n                ".join(str(c) for c in colors)
            lines.append('            "colors": [\n                ' + cl + "\n            ],")
        else:
            lines.append('            "colors": [],')
        lines.append(f'            "whNums": {cup["whNums"]},')
        lines.append(f'            "isVideo": {cup["isVideo"]},')
        lines.append(f'            "isLock": {cup["isLock"]},')
        lines.append(f'            "lockColor": {cup["lockColor"]},')
        lines.append(f'            "lockNums": {cup["lockNums"]},')
        lines.append(f'            "isNull": {cup["isNull"]}')
        if cup.get("isEmptyCup"):
            lines[-1] = lines[-1] + ","
            lines.append(f'            "isEmptyCup": {cup["isEmptyCup"]}')
        lines.append(f"        }}{comma}")
    lines.append("    ]")
    lines.append("}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> int:
    from_level = int(sys.argv[1]) if len(sys.argv) > 1 else 1
    to_level = int(sys.argv[2]) if len(sys.argv) > 2 else 99
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    ok, fail = 0, []
    for level in range(from_level, to_level + 1):
        doc, err = generate_level(level)
        if err:
            fail.append((level, err))
            print(f"  失败 第{level}关: {err}", flush=True)
            continue
        write_json(OUTPUT_DIR / f"level_{level}.json", doc)
        ok += 1
        if level % 10 == 0 or level <= 5:
            print(f"  已生成 {level}/99", flush=True)

    for path in OUTPUT_DIR.glob("level_*.json"):
        n = int(path.stem.split("_")[1])
        if n > MAX_LEVEL:
            path.unlink()

    print(f"完成：{from_level}~{to_level} 成功 {ok}，失败 {len(fail)}")
    for level, err in fail[:25]:
        print(f"  第 {level} 关: {err}")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
