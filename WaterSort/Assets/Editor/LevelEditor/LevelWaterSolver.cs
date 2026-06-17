using System.Collections.Generic;
using System.Text;
using AsGame.Core;
using AsGame.Data;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    public struct LevelSolveResult
    {
        public bool IsSolvable;
        public int MinSteps;
        public int SearchLimitSteps;
        public string Message;
    }

    /// <summary>
    /// 按正常游戏流程 BFS：倒水步数 + 自动装袋/锁瓶解锁（装袋不计步）。
    /// 参与：普通瓶、锁瓶、空瓶；不参与：空槽、广告瓶。
    /// </summary>
    public static class LevelWaterSolver
    {
        const int MaxCapacity = GameConstants.WaterMaxCount;
        const int PocketCount = 4;
        const int MaxDepth = 250;
        const int MaxVisited = 120000;

        public static LevelSolveResult TryFindMinSteps(
            IList<CupData> cups,
            int levelIndex = 1,
            int maxStepsLimit = 0,
            int maxVisitedLimit = 0)
        {
            var result = new LevelSolveResult();
            if (maxStepsLimit <= 0)
                maxStepsLimit = MaxDepth;
            var visitedLimit = maxVisitedLimit > 0
                ? maxVisitedLimit
                : maxStepsLimit <= LevelWaterCheckPolicy.MaxAllowedMinSteps
                    ? 30000
                    : MaxVisited;
            if (cups == null || cups.Count == 0)
            {
                result.Message = "无瓶子数据";
                return result;
            }

            if (!TryBuildInitialState(cups, levelIndex, out var initial, out var needCollect, out var error))
            {
                result.Message = error;
                return result;
            }

            if (needCollect == 0)
            {
                result.IsSolvable = true;
                result.MinSteps = 0;
                return result;
            }

            var startKey = initial.SerializeKey();
            var queue = new Queue<(SolverState state, int depth)>();
            var visited = new HashSet<string> { startKey };
            queue.Enqueue((initial.Clone(), 0));

            while (queue.Count > 0)
            {
                var (state, depth) = queue.Dequeue();
                if (state.Collected >= needCollect)
                {
                    result.IsSolvable = true;
                    result.MinSteps = depth;
                    return result;
                }

                if (depth >= maxStepsLimit)
                    continue;

                if (visited.Count >= visitedLimit)
                    break;

                for (var from = 0; from < state.Bottles.Count; from++)
                {
                    if (!state.CanPourFrom(from)) continue;
                    var pourRun = state.TopPourRun(from);
                    for (var to = 0; to < state.Bottles.Count; to++)
                    {
                        if (from == to || !state.CanPourTo(to)) continue;
                        if (!state.CanPour(from, to)) continue;

                        var move = Mathf.Min(pourRun, MaxCapacity - state.Bottles[to].Stack.Count);
                        if (move <= 0) continue;

                        var next = state.Clone();
                        next.ApplyPour(from, to, move);
                        next.ApplyAutoPackAndUnlock();

                        var key = next.SerializeKey();
                        if (!visited.Add(key)) continue;
                        queue.Enqueue((next, depth + 1));
                    }
                }
            }

            result.IsSolvable = false;
            result.SearchLimitSteps = maxStepsLimit;
            result.Message = visited.Count >= visitedLimit
                ? $"未在 {maxStepsLimit} 步 / {visitedLimit} 状态内找到解"
                : $"未在 {maxStepsLimit} 步内找到解（单关上限 {LevelWaterCheckPolicy.MaxAllowedMinSteps}）";
            return result;
        }

        static bool TryBuildInitialState(
            IList<CupData> cups,
            int levelIndex,
            out SolverState state,
            out int needCollect,
            out string error)
        {
            state = new SolverState();
            needCollect = 0;
            error = null;

            var totalLayers = 0;

            for (var i = 0; i < cups.Count; i++)
            {
                var cup = cups[i];
                if (cup == null || cup.isNull != 0 || cup.isVideo != 0)
                    continue;

                var sim = new BottleSim { SlotIndex = i };
                if (cup.isLock != 0)
                {
                    sim.IsLockBottle = true;
                    sim.LockColor = cup.lockColor;
                    sim.LockRemaining = cup.lockNums > 0 ? cup.lockNums : 1;
                }

                sim.WhNums = cup.whNums;
                sim.WhMask = CupWhLayerUtility.GetMask(cup);
                if (cup.colors != null)
                {
                    sim.Stack.AddRange(cup.colors);
                    totalLayers += cup.colors.Count;
                }

                state.Bottles.Add(sim);
            }

            if (state.Bottles.Count == 0)
            {
                error = "没有参与求解的瓶子";
                return false;
            }

            if (totalLayers % 4 != 0)
            {
                error = "参与瓶水层总数不是 4 的倍数";
                return false;
            }

            needCollect = totalLayers / 4;
            var pocketBuild = new List<int>();
            LevelPocketColorPlanner.BuildPocketColors(
                cups, levelIndex, pocketBuild, useUnityRandom: false);

            state.PocketRefill = pocketBuild;
            state.PocketLocked = new[] { false, false, true, true };
            state.PocketColors = new int[PocketCount];
            AssignInitialPocketColors(state);
            state.Collected = 0;
            state.ApplyAutoPackAndUnlock();
            return true;
        }

        static void AssignInitialPocketColors(SolverState state)
        {
            for (var p = 0; p < PocketCount; p++)
            {
                if (state.PocketLocked[p])
                {
                    state.PocketColors[p] = 0;
                    continue;
                }

                state.PocketColors[p] = state.PocketRefill.Count > 0 ? state.PocketRefill[0] : 0;
                if (state.PocketRefill.Count > 0)
                    state.PocketRefill.RemoveAt(0);
            }
        }

        sealed class BottleSim
        {
            public int SlotIndex;
            public bool Packed;
            public bool IsLockBottle;
            public int LockColor;
            public int LockRemaining;
            public int WhNums;
            public int WhMask;
            public List<int> Stack = new();

            public bool InPlay => !Packed;
            public bool IsLocked => IsLockBottle && LockRemaining > 0;

            public BottleSim CloneShallow()
            {
                return new BottleSim
                {
                    SlotIndex = SlotIndex,
                    Packed = Packed,
                    IsLockBottle = IsLockBottle,
                    LockColor = LockColor,
                    LockRemaining = LockRemaining,
                    WhNums = WhNums,
                    WhMask = WhMask,
                    Stack = new List<int>(Stack)
                };
            }
        }

        sealed class SolverState
        {
            public List<BottleSim> Bottles = new();
            public int Collected;
            public int[] PocketColors = new int[PocketCount];
            public bool[] PocketLocked = new bool[PocketCount];
            public List<int> PocketRefill = new();

            public SolverState Clone()
            {
                var clone = new SolverState
                {
                    Collected = Collected,
                    PocketColors = (int[])PocketColors.Clone(),
                    PocketLocked = (bool[])PocketLocked.Clone(),
                    PocketRefill = new List<int>(PocketRefill)
                };
                foreach (var b in Bottles)
                    clone.Bottles.Add(b.CloneShallow());
                return clone;
            }

            public bool CanPourFrom(int i)
            {
                var b = Bottles[i];
                return b.InPlay && !b.IsLocked && b.Stack.Count > 0 && !IsCollect(i);
            }

            public bool CanPourTo(int i)
            {
                var b = Bottles[i];
                return b.InPlay && !b.IsLocked && b.Stack.Count < MaxCapacity;
            }

            public bool CanPour(int from, int to) =>
                CanPourFrom(from) && CanPourTo(to) &&
                (Bottles[to].Stack.Count == 0 || Bottles[from].Stack[^1] == Bottles[to].Stack[^1]);

            public int TopPourRun(int from)
            {
                var stack = Bottles[from].Stack;
                if (stack.Count == 0) return 0;
                var color = stack[^1];
                var run = 0;
                for (var i = stack.Count - 1; i >= 0; i--)
                {
                    if (IsHiddenLayer(from, i)) break;
                    if (stack[i] != color) break;
                    run++;
                }

                return run;
            }

            bool IsHiddenLayer(int bottleIndex, int layerIndex)
            {
                var b = Bottles[bottleIndex];
                return b.Stack.Count > 1 && (b.WhMask & (1 << layerIndex)) != 0;
            }

            public bool IsCollect(int i)
            {
                var b = Bottles[i];
                if (!b.InPlay || b.Stack.Count != MaxCapacity || b.WhMask != 0) return false;
                var c = b.Stack[0];
                for (var l = 1; l < b.Stack.Count; l++)
                {
                    if (b.Stack[l] != c) return false;
                }

                return true;
            }

            public void ApplyPour(int from, int to, int move)
            {
                for (var m = 0; m < move; m++)
                {
                    var color = Bottles[from].Stack[^1];
                    Bottles[from].Stack.RemoveAt(Bottles[from].Stack.Count - 1);
                    Bottles[to].Stack.Add(color);
                }
            }

            public void ApplyAutoPackAndUnlock()
            {
                var guard = 0;
                while (guard++ < 64)
                {
                    if (TryPackOne())
                        continue;
                    if (!TryUnlockPocketForPendingCollect())
                        break;
                }
            }

            bool HasOpenPocketFor(int color)
            {
                for (var p = 0; p < PocketCount; p++)
                {
                    if (PocketLocked[p]) continue;
                    if (PocketColors[p] == color) return true;
                }

                return false;
            }

            bool TryUnlockPocketForPendingCollect()
            {
                for (var i = 0; i < Bottles.Count; i++)
                {
                    if (!IsCollect(i) || Bottles[i].IsLocked) continue;
                    var color = Bottles[i].Stack[^1];
                    if (HasOpenPocketFor(color)) continue;
                    return UnlockNextPocket();
                }

                return false;
            }

            bool UnlockNextPocket()
            {
                for (var p = 0; p < PocketCount; p++)
                {
                    if (!PocketLocked[p]) continue;
                    PocketLocked[p] = false;
                    PocketColors[p] = PocketRefill.Count > 0 ? PocketRefill[0] : 0;
                    if (PocketRefill.Count > 0)
                        PocketRefill.RemoveAt(0);
                    return true;
                }

                return false;
            }

            bool TryPackOne()
            {
                for (var p = 0; p < PocketCount; p++)
                {
                    if (PocketLocked[p]) continue;
                    var need = PocketColors[p];
                    if (need <= 0) continue;

                    for (var i = 0; i < Bottles.Count; i++)
                    {
                        if (!IsCollect(i) || IsLocked(i)) continue;
                        if (Bottles[i].Stack[^1] != need) continue;
                        PackBottle(i, p);
                        return true;
                    }
                }

                return false;
            }

            bool IsLocked(int i) => Bottles[i].IsLocked;

            void PackBottle(int bottleIndex, int pocketIndex)
            {
                var color = Bottles[bottleIndex].Stack[^1];
                Bottles[bottleIndex].Packed = true;
                Bottles[bottleIndex].Stack.Clear();
                Collected++;
                ApplyLockUnlockOnPack(color);
                PocketColors[pocketIndex] = PocketRefill.Count > 0 ? PocketRefill[0] : 0;
                if (PocketRefill.Count > 0)
                    PocketRefill.RemoveAt(0);
            }

            void ApplyLockUnlockOnPack(int packedColor)
            {
                foreach (var b in Bottles)
                {
                    if (!b.IsLockBottle || b.LockRemaining <= 0) continue;
                    if (b.LockColor != 0 && b.LockColor != packedColor) continue;
                    b.LockRemaining--;
                }
            }

            public string SerializeKey()
            {
                var sb = new StringBuilder(256);
                sb.Append('C').Append(Collected).Append(';');
                for (var p = 0; p < PocketCount; p++)
                    sb.Append(PocketColors[p]).Append(PocketLocked[p] ? 'L' : 'U').Append(',');
                sb.Append('|');
                foreach (var r in PocketRefill)
                    sb.Append(r).Append(',');
                sb.Append('|');
                foreach (var b in Bottles)
                {
                    sb.Append(b.Packed ? 'P' : 'A');
                    sb.Append(b.LockRemaining).Append(':');
                    foreach (var c in b.Stack)
                        sb.Append(c).Append(',');
                    sb.Append(';');
                }

                return sb.ToString();
            }
        }
    }
}
