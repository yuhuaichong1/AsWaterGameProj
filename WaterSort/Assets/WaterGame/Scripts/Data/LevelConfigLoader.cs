using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using AsGame.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AsGame.Data
{
    public static class LevelConfigLoader
    {
        const string LegacyResourcePath = "Levels/ConfTotal";

        static TextAsset _cachedAsset;
        static Dictionary<string, List<List<object>>> _rawLevels;
        static HashSet<int> _splitLevelIndices;
        static readonly Dictionary<int, List<CupData>> _splitCache = new();
        static bool _splitIndexBuilt;

        public static bool UsesSplitLevels
        {
            get
            {
                EnsureSplitIndex();
                return _splitLevelIndices != null && _splitLevelIndices.Count > 0;
            }
        }

        public static IReadOnlyCollection<int> GetSplitLevelIndices()
        {
            EnsureSplitIndex();
            return _splitLevelIndices ?? (IReadOnlyCollection<int>)Array.Empty<int>();
        }

        public static List<CupData> LoadLevel(int levelIndex)
        {
            EnsureSplitIndex();
            if (UsesSplitLevels)
                return LoadSplitLevel(levelIndex);

            return LoadFromConfTotal(levelIndex);
        }

        public static int GetLevelCount()
        {
            EnsureSplitIndex();
            if (UsesSplitLevels)
                return _splitLevelIndices.Count;

            EnsureConfTotalLoaded();
            return _rawLevels?.Count ?? 0;
        }

        public static int GetMaxSplitLevelIndex()
        {
            EnsureSplitIndex();
            if (_splitLevelIndices == null || _splitLevelIndices.Count == 0)
                return 0;
            return _splitLevelIndices.Max();
        }

        public static void InvalidateCache()
        {
            _cachedAsset = null;
            _rawLevels = null;
            _splitLevelIndices = null;
            _splitIndexBuilt = false;
            _splitCache.Clear();
        }

#if UNITY_EDITOR
        /// <summary>编辑器：保存单关 JSON 到 Split 目录。</summary>
        public static bool SaveSplitLevel(int levelIndex, IReadOnlyList<CupData> cups)
        {
            try
            {
                Directory.CreateDirectory(ProjectPaths.LevelsSplitAbsolute);
                var path = ProjectPaths.GetSplitLevelAbsolute(levelIndex);
                var json = LevelJsonCodec.ToJson(levelIndex, cups, prettyPrint: true);
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[LevelConfigLoader] 保存关卡失败: " + ex.Message);
                return false;
            }
        }

        /// <summary>编辑器：从 Split 文件读取（不走缓存）。</summary>
        public static List<CupData> LoadSplitLevelFromDisk(int levelIndex)
        {
            var path = ProjectPaths.GetSplitLevelAbsolute(levelIndex);
            if (!File.Exists(path))
                return new List<CupData>();
            return LevelJsonCodec.FromJson(File.ReadAllText(path));
        }

        /// <summary>编辑器：将 ConfTotal 拆分为单关 JSON。</summary>
        public static int ExportConfTotalToSplit(string confTotalJson = null)
        {
            confTotalJson ??= LoadLegacyJsonText();
            if (string.IsNullOrEmpty(confTotalJson))
            {
                Debug.LogError("[LevelConfigLoader] 未找到 ConfTotal，无法拆分。");
                return 0;
            }

            var parsed = ParseConfTotal(confTotalJson);
            Directory.CreateDirectory(ProjectPaths.LevelsSplitAbsolute);
            var count = 0;
            foreach (var kv in parsed.OrderBy(k => ParseLevelNumber(k.Key)))
            {
                var levelNum = ParseLevelNumber(kv.Key);
                if (levelNum <= 0) continue;
                var cups = MapCupData(kv.Value);
                var path = ProjectPaths.GetSplitLevelAbsolute(levelNum);
                File.WriteAllText(path, LevelJsonCodec.ToJson(levelNum, cups, prettyPrint: true));
                count++;
            }

            AssetDatabase.Refresh();
            InvalidateCache();
            Debug.Log($"[LevelConfigLoader] 已拆分 {count} 关 → {ProjectPaths.LevelsSplitAssetPath}");
            return count;
        }
#endif

        static List<CupData> LoadSplitLevel(int levelIndex)
        {
            if (_splitCache.TryGetValue(levelIndex, out var cached))
                return CloneCupList(cached);

            var json = LoadSplitLevelJsonText(levelIndex);
            if (!string.IsNullOrEmpty(json))
            {
                var cups = LevelJsonCodec.FromJson(json);
                _splitCache[levelIndex] = cups;
                return CloneCupList(cups);
            }

            var max = GetMaxSplitLevelIndex();
            if (max <= 0)
                return new List<CupData>();

            if (levelIndex > max)
            {
                var loopStart = 15;
                var loopCount = Math.Max(1, max - loopStart + 1);
                var loopIndex = loopStart + (levelIndex - max - 1) % loopCount;
                return LoadSplitLevel(loopIndex);
            }

            return LoadSplitLevel(Math.Min(levelIndex, max));
        }

        static List<CupData> LoadFromConfTotal(int levelIndex)
        {
            EnsureConfTotalLoaded();
            var key = "level_" + levelIndex;
            if (!_rawLevels.TryGetValue(key, out var raw) || raw == null)
            {
                var total = _rawLevels.Count;
                if (total <= 15)
                    return new List<CupData>();
                var loop = (levelIndex - total) % Math.Max(1, total - 15);
                key = "level_" + (15 + loop);
                if (!_rawLevels.TryGetValue(key, out raw))
                    raw = _rawLevels["level_1"];
            }

            return MapCupData(raw);
        }

        static void EnsureSplitIndex()
        {
            if (_splitIndexBuilt) return;
            _splitIndexBuilt = true;
            _splitLevelIndices = new HashSet<int>();

            foreach (var path in EnumerateSplitFilesOnDisk())
                TryAddSplitIndex(path);

#if UNITY_EDITOR
            if (_splitLevelIndices.Count == 0)
            {
                var assetDir = ProjectPaths.LevelsSplitAssetPath;
                if (AssetDatabase.IsValidFolder(assetDir.Replace('\\', '/').TrimEnd('/')))
                {
                    foreach (var guid in AssetDatabase.FindAssets("level_ t:TextAsset", new[] { ProjectPaths.LevelsSplitAssetPath }))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        TryAddSplitIndex(path);
                    }
                }
            }
#endif

            if (_splitLevelIndices.Count == 0)
            {
                var all = Resources.LoadAll<TextAsset>(ProjectPaths.LevelsSplitResourceFolder);
                if (all != null)
                {
                    foreach (var asset in all)
                    {
                        if (asset == null) continue;
                        var m = Regex.Match(asset.name, @"^level_(\d+)$");
                        if (m.Success)
                            _splitLevelIndices.Add(int.Parse(m.Groups[1].Value));
                    }
                }
            }

            if (_splitLevelIndices.Count > 0)
                Debug.Log($"[LevelConfigLoader] 拆关模式：{_splitLevelIndices.Count} 关 ({ProjectPaths.LevelsSplitResourceFolder})");
        }

        static void TryAddSplitIndex(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var m = Regex.Match(name, @"^level_(\d+)$");
            if (m.Success)
                _splitLevelIndices.Add(int.Parse(m.Groups[1].Value));
        }

        static IEnumerable<string> EnumerateSplitFilesOnDisk()
        {
            var dir = ProjectPaths.LevelsSplitAbsolute;
            if (!Directory.Exists(dir))
                yield break;
            foreach (var file in Directory.GetFiles(dir, "level_*.json"))
                yield return file;
        }

        static string LoadSplitLevelJsonText(int levelIndex)
        {
            var resourcePath = ProjectPaths.GetSplitLevelResourcePath(levelIndex);
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null && !string.IsNullOrEmpty(asset.text))
                return asset.text;

#if UNITY_EDITOR
            var editorPath = ProjectPaths.GetSplitLevelAssetPath(levelIndex);
            var editorAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(editorPath);
            if (editorAsset != null && !string.IsNullOrEmpty(editorAsset.text))
                return editorAsset.text;
#endif

            var diskPath = ProjectPaths.GetSplitLevelAbsolute(levelIndex);
            if (File.Exists(diskPath))
            {
                try
                {
                    return File.ReadAllText(diskPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LevelConfigLoader] 读取拆关文件失败 " + diskPath + ": " + ex.Message);
                }
            }

            return null;
        }

        static void EnsureConfTotalLoaded()
        {
            if (_rawLevels != null) return;

            var json = LoadLegacyJsonText();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning(
                    "[LevelConfigLoader] 未找到 ConfTotal；请使用拆关目录或放置 " +
                    ProjectPaths.ToEditorAssetPath("Levels/ConfTotal.json"));
                _rawLevels = new Dictionary<string, List<List<object>>>();
                return;
            }

            _rawLevels = ParseConfTotal(json);
            if (_rawLevels.Count == 0)
                Debug.LogWarning("[LevelConfigLoader] ConfTotal 解析到 0 关。");
            else if (!UsesSplitLevels)
                Debug.Log($"[LevelConfigLoader] ConfTotal 模式：{_rawLevels.Count} 关");
        }

        static string LoadLegacyJsonText()
        {
            _cachedAsset = Resources.Load<TextAsset>(LegacyResourcePath);
            if (_cachedAsset != null && !string.IsNullOrEmpty(_cachedAsset.text))
                return _cachedAsset.text;

            var all = Resources.LoadAll<TextAsset>("Levels");
            if (all != null)
            {
                foreach (var asset in all)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.text)) continue;
                    if (asset.name.Equals("ConfTotal", StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedAsset = asset;
                        return asset.text;
                    }
                }
            }

#if UNITY_EDITOR
            var editorJson = TryLoadConfTotalFromAssetDatabase();
            if (!string.IsNullOrEmpty(editorJson))
                return editorJson;
#endif

            foreach (var path in GetConfTotalDiskCandidates())
            {
                if (!File.Exists(path)) continue;
                try
                {
                    return File.ReadAllText(path);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LevelConfigLoader] 读取失败 " + path + ": " + ex.Message);
                }
            }

            return null;
        }

#if UNITY_EDITOR
        static string TryLoadConfTotalFromAssetDatabase()
        {
            var direct = ProjectPaths.ToEditorAssetPath("Levels/ConfTotal.json");
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(direct);
            if (asset != null && !string.IsNullOrEmpty(asset.text))
            {
                _cachedAsset = asset;
                return asset.text;
            }

            foreach (var guid in AssetDatabase.FindAssets("ConfTotal t:TextAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Replace('\\', '/').EndsWith("/Levels/ConfTotal.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null || string.IsNullOrEmpty(asset.text)) continue;
                _cachedAsset = asset;
                return asset.text;
            }

            return null;
        }
#endif

        static IEnumerable<string> GetConfTotalDiskCandidates()
        {
            foreach (var resourcesRoot in ProjectPaths.EnumerateResourcesRootsOnDisk())
                yield return Path.Combine(resourcesRoot, "Levels", "ConfTotal.json");

            yield return ProjectPaths.LevelsConfTotalAbsolute;
            yield return Path.Combine(Application.streamingAssetsPath, "Levels", "ConfTotal.json");
        }

        static int ParseLevelNumber(string key)
        {
            var m = Regex.Match(key ?? "", @"level_(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        static List<CupData> CloneCupList(List<CupData> source)
        {
            if (source == null) return new List<CupData>();
            var list = new List<CupData>(source.Count);
            for (var i = 0; i < source.Count; i++)
                list.Add(source[i].Clone());
            return list;
        }

        /// <summary>
        /// 解析 ConfTotal.json：level_N -> [[pos, colors, wh, video, lock, lockColor, lockNums], ...]
        /// </summary>
        public static Dictionary<string, List<List<object>>> ParseConfTotal(string json)
        {
            var result = new Dictionary<string, List<List<object>>>();
            var levelMatches = Regex.Matches(json, "\"(level_\\d+)\"\\s*:\\s*\\[");
            for (var i = 0; i < levelMatches.Count; i++)
            {
                var key = levelMatches[i].Groups[1].Value;
                var arrayStart = levelMatches[i].Index + levelMatches[i].Length - 1;
                var arrayEnd = FindMatchingBracket(json, arrayStart);
                if (arrayEnd < 0) continue;
                var levelJson = json.Substring(arrayStart, arrayEnd - arrayStart + 1);
                result[key] = ParseLevelArray(levelJson);
            }

            return result;
        }

        static List<List<object>> ParseLevelArray(string json)
        {
            var cups = new List<List<object>>();
            var depth = 0;
            var cupStart = -1;
            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '[')
                {
                    if (depth == 1)
                        cupStart = i;
                    depth++;
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 1 && cupStart >= 0)
                    {
                        var cupJson = json.Substring(cupStart, i - cupStart + 1);
                        cups.Add(ParseCupEntry(cupJson));
                        cupStart = -1;
                    }
                }
            }

            return cups;
        }

        static List<object> ParseCupEntry(string cupJson)
        {
            var posMatch = Regex.Match(cupJson, "\\{\\s*\"x\"\\s*:\\s*(-?[\\d.]+)\\s*,\\s*\"y\"\\s*:\\s*(-?[\\d.]+)\\s*\\}");
            var pos = new Dictionary<string, float>
            {
                { "x", posMatch.Success ? float.Parse(posMatch.Groups[1].Value) : 0f },
                { "y", posMatch.Success ? float.Parse(posMatch.Groups[2].Value) : 0f }
            };

            var colorsMatch = Regex.Match(cupJson, "\\[\\s*((?:\\d+\\s*,\\s*)*\\d+)?\\s*\\]", RegexOptions.Singleline);
            var colors = new List<int>();
            if (colorsMatch.Success)
            {
                var inner = colorsMatch.Value.Trim('[', ']', ' ');
                if (!string.IsNullOrEmpty(inner))
                {
                    foreach (var part in inner.Split(','))
                    {
                        if (int.TryParse(part.Trim(), out var colorId))
                            colors.Add(colorId);
                    }
                }
            }

            var tailMatch = Regex.Match(cupJson,
                "\\]\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)(?:\\s*,\\s*(\\d+))?\\s*\\]");
            var wh = 0;
            var video = 0;
            var isLock = 0;
            var lockColor = 0;
            var lockNums = 0;
            var isNull = 0;
            if (tailMatch.Success)
            {
                wh = int.Parse(tailMatch.Groups[1].Value);
                video = int.Parse(tailMatch.Groups[2].Value);
                isLock = int.Parse(tailMatch.Groups[3].Value);
                lockColor = int.Parse(tailMatch.Groups[4].Value);
                lockNums = int.Parse(tailMatch.Groups[5].Value);
                if (tailMatch.Groups[6].Success)
                    isNull = int.Parse(tailMatch.Groups[6].Value);
            }

            return new List<object> { pos, colors, wh, video, isLock, lockColor, lockNums, isNull };
        }

        static int FindMatchingBracket(string text, int startIndex)
        {
            var depth = 0;
            for (var i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '[') depth++;
                else if (text[i] == ']')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }

            return -1;
        }

        public static List<CupData> MapCupData(List<List<object>> raw)
        {
            var list = new List<CupData>();
            if (raw == null) return list;
            for (var i = 0; i < raw.Count; i++)
            {
                var entry = raw[i];
                if (entry == null || entry.Count < 2) continue;
                var posDict = entry[0] as Dictionary<string, float>;
                var colors = entry[1] as List<int> ?? new List<int>();
                list.Add(new CupData
                {
                    id = i,
                    position = posDict != null ? new Vector2(posDict["x"], posDict["y"]) : Vector2.zero,
                    colors = new List<int>(colors),
                    whNums = entry.Count > 2 ? Convert.ToInt32(entry[2]) : 0,
                    isVideo = entry.Count > 3 ? Convert.ToInt32(entry[3]) : 0,
                    isLock = entry.Count > 4 ? Convert.ToInt32(entry[4]) : 0,
                    lockColor = entry.Count > 5 ? Convert.ToInt32(entry[5]) : 0,
                    lockNums = entry.Count > 6 ? Convert.ToInt32(entry[6]) : 0,
                    isNull = entry.Count > 7 ? Convert.ToInt32(entry[7]) : 0,
                    isEmptyCup = 0
                });
            }

            return list;
        }
    }
}
