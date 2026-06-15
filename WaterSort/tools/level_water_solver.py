"""BFS 最少步数求解（与 LevelWaterSolver.cs 规则一致）。"""

from __future__ import annotations

import math
from collections import deque
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple

MAX_CAPACITY = 4
POCKET_COUNT = 4
MAX_DEPTH = 250
MAX_VISITED = 120000


@dataclass
class SolveResult:
    is_solvable: bool = False
    min_steps: int = 0
    search_limit_steps: int = 0
    message: str = ""


@dataclass
class BottleSim:
    is_lock_bottle: bool = False
    lock_color: int = 0
    lock_remaining: int = 0
    wh_nums: int = 0
    packed: bool = False
    stack: List[int] = field(default_factory=list)

    @property
    def in_play(self) -> bool:
        return not self.packed

    @property
    def is_locked(self) -> bool:
        return self.is_lock_bottle and self.lock_remaining > 0


class SolverState:
    def __init__(self) -> None:
        self.bottles: List[BottleSim] = []
        self.collected = 0
        self.pocket_colors = [0] * POCKET_COUNT
        self.pocket_locked = [False, False, True, True]
        self.pocket_refill: List[int] = []

    def clone(self) -> "SolverState":
        s = SolverState()
        s.collected = self.collected
        s.pocket_colors = list(self.pocket_colors)
        s.pocket_locked = list(self.pocket_locked)
        s.pocket_refill = list(self.pocket_refill)
        for b in self.bottles:
            s.bottles.append(BottleSim(
                is_lock_bottle=b.is_lock_bottle,
                lock_color=b.lock_color,
                lock_remaining=b.lock_remaining,
                wh_nums=b.wh_nums,
                packed=b.packed,
                stack=list(b.stack),
            ))
        return s

    def serialize_key(self) -> str:
        parts = [f"C{self.collected};"]
        for p in range(POCKET_COUNT):
            parts.append(f"{self.pocket_colors[p]}{'L' if self.pocket_locked[p] else 'U'},")
        parts.append("|")
        parts.extend(f"{r}," for r in self.pocket_refill)
        parts.append("|")
        for b in self.bottles:
            parts.append("P" if b.packed else "A")
            parts.append(f"{b.lock_remaining}:")
            parts.extend(f"{c}," for c in b.stack)
            parts.append(";")
        return "".join(parts)

    def is_hidden_layer(self, bi: int, li: int) -> bool:
        b = self.bottles[bi]
        return b.wh_nums > 0 and len(b.stack) > 1 and li < b.wh_nums

    def is_collect(self, i: int) -> bool:
        b = self.bottles[i]
        if not b.in_play or len(b.stack) != MAX_CAPACITY or b.wh_nums > 0:
            return False
        c = b.stack[0]
        return all(x == c for x in b.stack)

    def can_pour_from(self, i: int) -> bool:
        b = self.bottles[i]
        return b.in_play and not b.is_locked and b.stack and not self.is_collect(i)

    def can_pour_to(self, i: int) -> bool:
        b = self.bottles[i]
        return b.in_play and not b.is_locked and len(b.stack) < MAX_CAPACITY

    def can_pour(self, fr: int, to: int) -> bool:
        if not self.can_pour_from(fr) or not self.can_pour_to(to):
            return False
        if not self.bottles[to].stack:
            return True
        return self.bottles[fr].stack[-1] == self.bottles[to].stack[-1]

    def top_pour_run(self, fr: int) -> int:
        stack = self.bottles[fr].stack
        if not stack:
            return 0
        color = stack[-1]
        run = 0
        for i in range(len(stack) - 1, -1, -1):
            if self.is_hidden_layer(fr, i):
                break
            if stack[i] != color:
                break
            run += 1
        return run

    def apply_pour(self, fr: int, to: int, move: int) -> None:
        for _ in range(move):
            color = self.bottles[fr].stack.pop()
            self.bottles[to].stack.append(color)

    def has_open_pocket_for(self, color: int) -> bool:
        for p in range(POCKET_COUNT):
            if not self.pocket_locked[p] and self.pocket_colors[p] == color:
                return True
        return False

    def unlock_next_pocket(self) -> bool:
        for p in range(POCKET_COUNT):
            if not self.pocket_locked[p]:
                continue
            self.pocket_locked[p] = False
            self.pocket_colors[p] = self.pocket_refill[0] if self.pocket_refill else 0
            if self.pocket_refill:
                self.pocket_refill.pop(0)
            return True
        return False

    def try_unlock_pocket_for_pending_collect(self) -> bool:
        for i, b in enumerate(self.bottles):
            if not self.is_collect(i) or b.is_locked:
                continue
            color = b.stack[-1]
            if self.has_open_pocket_for(color):
                continue
            return self.unlock_next_pocket()
        return False

    def apply_lock_unlock_on_pack(self, packed_color: int) -> None:
        for b in self.bottles:
            if not b.is_lock_bottle or b.lock_remaining <= 0:
                continue
            if b.lock_color != 0 and b.lock_color != packed_color:
                continue
            b.lock_remaining -= 1

    def pack_bottle(self, bi: int, pi: int) -> None:
        color = self.bottles[bi].stack[-1]
        self.bottles[bi].packed = True
        self.bottles[bi].stack.clear()
        self.collected += 1
        self.apply_lock_unlock_on_pack(color)
        self.pocket_colors[pi] = self.pocket_refill[0] if self.pocket_refill else 0
        if self.pocket_refill:
            self.pocket_refill.pop(0)

    def try_pack_one(self) -> bool:
        for p in range(POCKET_COUNT):
            if self.pocket_locked[p]:
                continue
            need = self.pocket_colors[p]
            if need <= 0:
                continue
            for i, b in enumerate(self.bottles):
                if not self.is_collect(i) or b.is_locked:
                    continue
                if b.stack[-1] != need:
                    continue
                self.pack_bottle(i, p)
                return True
        return False

    def apply_auto_pack_and_unlock(self) -> None:
        for _ in range(64):
            if self.try_pack_one():
                continue
            if not self.try_unlock_pocket_for_pending_collect():
                break


def _shuffle(lst: List[int], seed: int) -> None:
    rng = __import__("random").Random(seed)
    for i in range(len(lst) - 1, 0, -1):
        j = rng.randint(0, i)
        lst[i], lst[j] = lst[j], lst[i]


def _build_initial(cups: List[dict], level_index: int) -> Tuple[Optional[SolverState], int, Optional[str]]:
    state = SolverState()
    pocket_build: List[int] = []
    color_count: Dict[int, int] = {}
    total_layers = 0

    for cup in cups:
        if cup.get("isNull") or cup.get("isVideo"):
            continue
        sim = BottleSim()
        if cup.get("isLock"):
            sim.is_lock_bottle = True
            sim.lock_color = cup.get("lockColor", 0)
            sim.lock_remaining = cup.get("lockNums", 0) or 1
        sim.wh_nums = cup.get("whNums", 0)
        colors = cup.get("colors") or []
        sim.stack = list(colors)
        total_layers += len(colors)
        for c in colors:
            color_count[c] = color_count.get(c, 0) + 1
            if color_count[c] % 4 == 0:
                pocket_build.append(c)
        state.bottles.append(sim)

    if not state.bottles:
        return None, 0, "没有参与求解的瓶子"
    if total_layers % 4:
        return None, 0, "参与瓶水层总数不是 4 的倍数"

    need_collect = total_layers // 4
    if level_index != 1:
        _shuffle(pocket_build, level_index * 7919 + 17)

    state.pocket_refill = pocket_build
    for p in range(POCKET_COUNT):
        if state.pocket_locked[p]:
            state.pocket_colors[p] = 0
        else:
            state.pocket_colors[p] = state.pocket_refill[0] if state.pocket_refill else 0
            if state.pocket_refill:
                state.pocket_refill.pop(0)

    state.collected = 0
    state.apply_auto_pack_and_unlock()
    return state, need_collect, None


def try_find_min_steps(
    cups: List[dict],
    level_index: int = 1,
    max_depth: int = MAX_DEPTH,
    max_visited: int = MAX_VISITED,
) -> SolveResult:
    result = SolveResult()
    if not cups:
        result.message = "无瓶子数据"
        return result

    initial, need_collect, err = _build_initial(cups, level_index)
    if err:
        result.message = err
        return result
    assert initial is not None

    if need_collect == 0:
        result.is_solvable = True
        return result

    visited = {initial.serialize_key()}
    queue: deque = deque([(initial, 0)])

    while queue:
        state, depth = queue.popleft()
        if state.collected >= need_collect:
            result.is_solvable = True
            result.min_steps = depth
            return result
        if depth >= max_depth:
            continue
        if len(visited) >= max_visited:
            break

        n = len(state.bottles)
        for fr in range(n):
            if not state.can_pour_from(fr):
                continue
            run = state.top_pour_run(fr)
            for to in range(n):
                if fr == to or not state.can_pour_to(to) or not state.can_pour(fr, to):
                    continue
                move = min(run, MAX_CAPACITY - len(state.bottles[to].stack))
                if move <= 0:
                    continue
                nxt = state.clone()
                nxt.apply_pour(fr, to, move)
                nxt.apply_auto_pack_and_unlock()
                key = nxt.serialize_key()
                if key in visited:
                    continue
                visited.add(key)
                queue.append((nxt, depth + 1))

    result.is_solvable = False
    result.search_limit_steps = max_depth
    result.message = (
        f"未在 {max_depth} 步 / {max_visited} 状态内找到解"
        if len(visited) >= max_visited
        else f"未在 {max_depth} 步内找到解"
    )
    return result
