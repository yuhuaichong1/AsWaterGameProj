using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AsGame.Core;
using AsGame.Data;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    public sealed class LevelWaterValidationResult
    {
        public int LevelIndex;
        public int ColorCount;
        /// <summary>可完成的「满瓶组」数量，= 总水层数 / 4。</summary>
        public int ColorSetCount;
        public int TotalLayers;
        public int ParticipatingCupCount;
        public int RegularCupCount;
        public int LockCupCount;
        public int EmptySlotCupCount;
        public int EmptyBottleCount;
        public int VideoCupCount;

        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();

        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>校验关卡水层：颜色层数为 4 的倍数、总层数为 4 的倍数、参与瓶容量足够；普通瓶禁止开局满瓶同色。</summary>
    public static class LevelWaterValidator
    {
        const int MaxCapacity = GameConstants.WaterMaxCount;
        const int MinPlayColor = 1;
        const int MaxPlayColor = 8;

        public static LevelWaterValidationResult Validate(
            IList<CupData> cups,
            int levelIndex = 0,
            bool checkSolvability = true)
        {
            var result = ValidateStructure(cups, levelIndex);
            if (result.IsValid && checkSolvability)
                ValidateSolvability(result, cups, levelIndex);
            return result;
        }

        /// <summary>仅检查水层配置，不跑最少步数搜索。</summary>
        public static LevelWaterValidationResult ValidateStructure(IList<CupData> cups, int levelIndex = 0)
        {
            var result = new LevelWaterValidationResult { LevelIndex = levelIndex };

            if (cups == null || cups.Count == 0)
            {
                result.Errors.Add("关卡没有任何瓶子数据。");
                return result;
            }

            var colorCounts = new Dictionary<int, int>();
            var participatingLayers = 0;
            var lockLayersActual = 0;

            for (var i = 0; i < cups.Count; i++)
            {
                var cup = cups[i];
                if (cup == null)
                {
                    result.Errors.Add($"瓶子 #{i} 数据为空。");
                    continue;
                }

                var kind = CupSlotKindUtility.GetKind(cup);
                var layerCount = cup.colors?.Count ?? 0;

                switch (kind)
                {
                    case CupSlotKind.空槽:
                        result.EmptySlotCupCount++;
                        if (layerCount > 0)
                            result.Warnings.Add($"#{i} 空槽不应含水层（当前 {layerCount} 层）。");
                        continue;
                    case CupSlotKind.空瓶:
                        result.EmptyBottleCount++;
                        if (layerCount > 0)
                            result.Warnings.Add($"#{i} 空瓶不应含水层（当前 {layerCount} 层）。");
                        continue;
                    case CupSlotKind.广告瓶:
                        result.VideoCupCount++;
                        if (layerCount > 0)
                            result.Warnings.Add($"#{i} 广告瓶含水 {layerCount} 层，不参与颜色/层数统计。");
                        continue;
                    case CupSlotKind.锁瓶:
                        result.ParticipatingCupCount++;
                        result.LockCupCount++;
                        ValidateParticipatingCup(result, cup, i, kind, colorCounts, ref participatingLayers, levelIndex);
                        lockLayersActual += cup.colors?.Count ?? 0;
                        break;
                    default:
                        result.ParticipatingCupCount++;
                        result.RegularCupCount++;
                        ValidateParticipatingCup(result, cup, i, kind, colorCounts, ref participatingLayers, levelIndex);
                        break;
                }
            }

            if (result.ParticipatingCupCount == 0)
            {
                result.Errors.Add("没有可配置水层的瓶子（普通瓶/锁瓶）。");
                return result;
            }

            result.TotalLayers = participatingLayers;
            result.ColorCount = colorCounts.Count;

            if (participatingLayers <= 0)
            {
                result.Errors.Add("参与随机的瓶子内没有任何水层。");
                return result;
            }

            if (participatingLayers % 4 != 0)
                result.Errors.Add($"参与瓶水层总数 {participatingLayers} 不是 4 的倍数。");
            else
                result.ColorSetCount = participatingLayers / 4;

            foreach (var kv in colorCounts.OrderBy(k => k.Key))
            {
                if (kv.Value == 0) continue;
                if (kv.Value % 4 != 0)
                {
                    var name = GetColorName(kv.Key);
                    result.Errors.Add(
                        $"颜色 {kv.Key}{name} 出现 {kv.Value} 层，应为 4 的倍数（4、8、12…）。");
                }
            }

            ValidateParticipatingCapacity(result, participatingLayers, lockLayersActual);
            return result;
        }

        static void ValidateLockCupLayersForLevel(
            LevelWaterValidationResult result, int index, int layerCount, int levelIndex)
        {
            var expected = LevelLockLayerPolicy.GetMandatoryLockLayers(levelIndex);
            if (expected > 0)
            {
                if (layerCount != expected)
                    result.Errors.Add(
                        $"#{index} 第 {levelIndex} 关锁瓶应为 {expected} 层水（当前 {layerCount} 层）。");
                return;
            }

            if (levelIndex is >= 11 and <= 40 && layerCount > 3)
                result.Errors.Add($"#{index} 第 {levelIndex} 关锁瓶建议不超过 3 层（当前 {layerCount} 层）。");

            if (levelIndex is >= 3 and <= 10 && layerCount >= MaxCapacity)
                result.Errors.Add($"#{index} 前 10 关锁瓶不应满 {MaxCapacity} 层（当前 {layerCount} 层）。");
        }

        static void ValidateSolvability(
            LevelWaterValidationResult result, IList<CupData> cups, int levelIndex)
        {
            if (HasLockDeadlockAtStart(cups))
            {
                result.Errors.Add("关卡不可解：锁瓶内颜色无法在不解锁的情况下先完成装袋（死锁）");
                return;
            }

            var solve = LevelWaterSolver.TryFindMinSteps(
                cups, levelIndex, LevelWaterCheckPolicy.MaxAllowedMinSteps);
            if (solve.IsSolvable)
                return;

            var msg = string.IsNullOrEmpty(solve.Message)
                ? $"关卡不可解或未在 {LevelWaterCheckPolicy.MaxAllowedMinSteps} 步内找到解"
                : solve.Message;
            result.Errors.Add(msg);
        }

        /// <summary>锁瓶未解锁时，外部是否至少能先装袋一次（必要条件）。</summary>
        public static bool HasLockDeadlockAtStart(IList<CupData> cups)
        {
            var lockCups = new List<CupData>();
            var outsideCounts = new Dictionary<int, int>();

            foreach (var cup in cups)
            {
                if (cup == null) continue;
                var kind = CupSlotKindUtility.GetKind(cup);
                if (kind is CupSlotKind.空槽 or CupSlotKind.广告瓶 or CupSlotKind.空瓶)
                    continue;
                if (kind == CupSlotKind.锁瓶 && cup.lockNums > 0)
                {
                    lockCups.Add(cup);
                    continue;
                }

                if (cup.colors == null) continue;
                foreach (var c in cup.colors)
                {
                    if (!outsideCounts.ContainsKey(c)) outsideCounts[c] = 0;
                    outsideCounts[c]++;
                }
            }

            if (lockCups.Count == 0)
                return false;

            foreach (var lockCup in lockCups)
            {
                var need = lockCup.lockColor;
                if (need == 0)
                {
                    var canPack = false;
                    foreach (var kv in outsideCounts)
                    {
                        if (kv.Value >= MaxCapacity)
                        {
                            canPack = true;
                            break;
                        }
                    }

                    if (!canPack)
                        return true;
                }
                else if (!outsideCounts.TryGetValue(need, out var n) || n < MaxCapacity)
                {
                    return true;
                }
            }

            return false;
        }

        static void ValidateParticipatingCup(
            LevelWaterValidationResult result,
            CupData cup,
            int index,
            CupSlotKind kind,
            Dictionary<int, int> colorCounts,
            ref int participatingLayers,
            int levelIndex)
        {
            var layers = cup.colors ?? new List<int>();
            var layerCount = layers.Count;
            participatingLayers += layerCount;

            if (layerCount > MaxCapacity)
                result.Errors.Add($"#{index} {KindLabel(kind)} 水层 {layerCount} 超过上限 {MaxCapacity}。");

            if (kind == CupSlotKind.锁瓶 && layerCount < 1)
                result.Errors.Add($"#{index} 锁瓶至少需要 1 层水（当前 {layerCount} 层）。");
            else if (kind == CupSlotKind.锁瓶 && layerCount > MaxCapacity)
                result.Errors.Add($"#{index} 锁瓶水层 {layerCount} 超过上限 {MaxCapacity}。");
            else if (kind == CupSlotKind.锁瓶)
                ValidateLockCupLayersForLevel(result, index, layerCount, levelIndex);

            if (cup.whNums < 0 || cup.whNums > layerCount)
                result.Errors.Add($"#{index} {KindLabel(kind)} 问号层数 whNums={cup.whNums} 超出范围 [0,{layerCount}]。");

            if (kind == CupSlotKind.锁瓶 && cup.lockColor > 0)
            {
                for (var l = 0; l < layers.Count; l++)
                {
                    if (layers[l] != cup.lockColor)
                    {
                        result.Errors.Add(
                            $"#{index} 锁瓶 lockColor={cup.lockColor}，但第 {l} 层颜色为 {layers[l]}。");
                        break;
                    }
                }
            }

            if (kind == CupSlotKind.普通瓶 && IsUniformFullBottle(layers))
            {
                var c = layers[0];
                result.Errors.Add(
                    $"#{index} 普通瓶已有 {MaxCapacity} 层同色（颜色 {c}{GetColorName(c)}），编辑阶段不允许满瓶同色。");
            }

            foreach (var color in layers)
            {
                if (color == 0)
                {
                    result.Errors.Add($"#{index} {KindLabel(kind)} 含有无效颜色 0。");
                    continue;
                }

                if (color < MinPlayColor || color > MaxPlayColor || !GameConstants.GameColorData.ContainsKey(color))
                {
                    result.Errors.Add($"#{index} {KindLabel(kind)} 含有无效颜色 ID {color}（有效范围 1~8）。");
                    continue;
                }

                if (!colorCounts.ContainsKey(color))
                    colorCounts[color] = 0;
                colorCounts[color]++;
            }
        }

        static void ValidateParticipatingCapacity(
            LevelWaterValidationResult result, int participatingLayers, int lockLayersActual)
        {
            if (lockLayersActual > participatingLayers)
            {
                result.Errors.Add(
                    $"锁瓶占用 {lockLayersActual} 层，超过参与瓶水层总数 {participatingLayers}。");
                return;
            }

            var regularLayers = participatingLayers - lockLayersActual;
            var regularCapacity = result.RegularCupCount * MaxCapacity;

            if (regularLayers > regularCapacity)
            {
                result.Errors.Add(
                    $"普通瓶最多容纳 {regularCapacity} 层（{result.RegularCupCount} 瓶×4），" +
                    $"当前需 {regularLayers} 层（总 {participatingLayers} 层，锁瓶占 {lockLayersActual} 层）。");
            }

            if (regularLayers > 0 && result.RegularCupCount == 0)
            {
                result.Errors.Add($"除锁瓶外还有 {regularLayers} 层水，但没有普通瓶可承载。");
                return;
            }

            if (regularLayers > 0)
            {
                var minRegularCups = Mathf.CeilToInt(regularLayers / (float)MaxCapacity);
                if (result.RegularCupCount < minRegularCups)
                {
                    result.Errors.Add(
                        $"至少需要 {minRegularCups} 个普通瓶才能放下 {regularLayers} 层水，当前 {result.RegularCupCount} 个。");
                }
            }
        }

        public static List<LevelWaterValidationResult> ValidateAllOnDisk()
        {
            var results = new List<LevelWaterValidationResult>();
            var dir = ProjectPaths.LevelsSplitAbsolute;
            if (!Directory.Exists(dir))
                return results;

            var indices = new List<int>();
            foreach (var file in Directory.GetFiles(dir, "level_*.json"))
            {
                var m = Regex.Match(Path.GetFileNameWithoutExtension(file), @"^level_(\d+)$");
                if (m.Success)
                    indices.Add(int.Parse(m.Groups[1].Value));
            }

            indices.Sort();
            foreach (var level in indices)
            {
                var cups = LevelConfigLoader.LoadSplitLevelFromDisk(level);
                var result = Validate(cups, level);
                results.Add(result);
            }

            return results;
        }

        public static string FormatReport(LevelWaterValidationResult result)
        {
            var sb = new StringBuilder();
            var prefix = result.LevelIndex > 0 ? $"第 {result.LevelIndex} 关" : "当前关卡";

            if (result.IsValid)
            {
                sb.AppendLine($"✓ {prefix} 检查通过");
                sb.Append(
                    $"颜色 {result.ColorCount} 种，水层 {result.TotalLayers} 层（{result.ColorSetCount} 组×4）；" +
                    $"参与瓶 {result.ParticipatingCupCount}（普通 {result.RegularCupCount}，锁 {result.LockCupCount}）");
                if (result.EmptyBottleCount + result.EmptySlotCupCount + result.VideoCupCount > 0)
                {
                    sb.Append(
                        $"；其它：空瓶 {result.EmptyBottleCount}，空槽 {result.EmptySlotCupCount}，广告 {result.VideoCupCount}");
                }
            }
            else
            {
                sb.AppendLine($"✗ {prefix} 检查未通过（{result.Errors.Count} 项错误）");
                sb.AppendLine(
                    $"统计：颜色 {result.ColorCount} 种，水层 {result.TotalLayers}（{result.ColorSetCount} 组×4），" +
                    $"参与瓶 {result.ParticipatingCupCount}（普通 {result.RegularCupCount}，锁 {result.LockCupCount}）");
                foreach (var err in result.Errors)
                    sb.AppendLine("· " + err);
            }

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine($"提示（{result.Warnings.Count} 项）：");
                foreach (var warn in result.Warnings)
                    sb.AppendLine("· " + warn);
            }

            return sb.ToString().TrimEnd();
        }

        public static string FormatBatchReport(IReadOnlyList<LevelWaterValidationResult> results)
        {
            if (results == null || results.Count == 0)
                return "未找到任何关卡 JSON（Assets/AssetBundleLocal/Json/Levels/level_*.json）。";

            var valid = results.Count(r => r.IsValid);
            var invalid = results.Count - valid;
            var sb = new StringBuilder();
            sb.AppendLine($"全部检查完成：共 {results.Count} 关，合法 {valid} 关，不合法 {invalid} 关。");

            if (invalid == 0)
            {
                sb.Append("所有关卡水层、颜色倍数与参与瓶容量均匹配。");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine("不合法关卡：");
            foreach (var r in results.Where(r => !r.IsValid))
            {
                var first = r.Errors.Count > 0 ? r.Errors[0] : "未知错误";
                sb.AppendLine($"· 第 {r.LevelIndex} 关：{first}");
            }

            var withWarnings = results.Where(r => r.IsValid && r.Warnings.Count > 0).ToList();
            if (withWarnings.Count > 0)
            {
                sb.AppendLine($"另有 {withWarnings.Count} 关通过但有提示（如空瓶/广告瓶含水）。");
            }

            return sb.ToString().TrimEnd();
        }

        static string KindLabel(CupSlotKind kind) => kind switch
        {
            CupSlotKind.锁瓶 => "锁瓶",
            CupSlotKind.普通瓶 => "普通瓶",
            _ => kind.ToString()
        };

        static string GetColorName(int colorId) =>
            GameConstants.GameColorData.TryGetValue(colorId, out var pair) ? $"({pair.Name})" : "";

        /// <summary>满 4 层且全部为同一有效颜色（不含 0）。</summary>
        static bool IsUniformFullBottle(IList<int> layers)
        {
            if (layers == null || layers.Count != MaxCapacity)
                return false;
            var first = layers[0];
            if (first < MinPlayColor || first > MaxPlayColor)
                return false;
            for (var i = 1; i < layers.Count; i++)
            {
                if (layers[i] != first)
                    return false;
            }

            return true;
        }
    }
}
