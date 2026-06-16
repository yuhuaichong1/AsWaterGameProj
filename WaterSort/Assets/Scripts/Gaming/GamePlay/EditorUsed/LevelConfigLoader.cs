using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using AsGame.Core;
using XrCode;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AsGame.Data
{
    public static class LevelConfigLoader
    {
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
            return _splitLevelIndices != null
                ? (IReadOnlyCollection<int>)_splitLevelIndices
                : Array.Empty<int>();
        }

        public static int GetLevelCount()
        {
            EnsureSplitIndex();
            return _splitLevelIndices?.Count ?? 0;
        }

        public static int GetMaxSplitLevelIndex()
        {
            EnsureSplitIndex();
            return _splitLevelIndices == null || _splitLevelIndices.Count == 0 ? 0 : _splitLevelIndices.Max();
        }

        public static List<CupData> LoadLevel(int levelIndex)
        {
            EnsureSplitIndex();
            return LoadSplitLevel(levelIndex);
        }

        public static void InvalidateCache()
        {
            _splitLevelIndices = null;
            _splitIndexBuilt = false;
            _splitCache.Clear();
        }

#if UNITY_EDITOR
        public static bool SaveSplitLevel(int levelIndex, IReadOnlyList<CupData> cups)
        {
            try
            {
                Directory.CreateDirectory(ProjectPaths.LevelsSplitAbsolute);
                File.WriteAllText(
                    ProjectPaths.GetSplitLevelAbsolute(levelIndex),
                    LevelJsonCodec.ToJson(levelIndex, cups, prettyPrint: true));
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

        public static List<CupData> LoadSplitLevelFromDisk(int levelIndex)
        {
            var path = ProjectPaths.GetSplitLevelAbsolute(levelIndex);
            return File.Exists(path) ? LevelJsonCodec.FromJson(File.ReadAllText(path)) : new List<CupData>();
        }

        public static int ExportConfTotalToSplit(string confTotalJson = null)
        {
            confTotalJson ??= LoadConfTotalText();
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
                File.WriteAllText(
                    ProjectPaths.GetSplitLevelAbsolute(levelNum),
                    LevelJsonCodec.ToJson(levelNum, cups, prettyPrint: true));
                count++;
            }

            AssetDatabase.Refresh();
            InvalidateCache();
            Debug.Log($"[LevelConfigLoader] 已拆分 {count} 关 -> {ProjectPaths.LevelsSplitAssetPath}");
            return count;
        }
#endif

        static List<CupData> LoadSplitLevel(int levelIndex)
        {
            if (_splitCache.TryGetValue(levelIndex, out var cached))
                return CloneCupList(cached);

            var json = LoadSplitLevelJsonText(levelIndex);
            if (string.IsNullOrEmpty(json))
                return new List<CupData>();

            var cups = LevelJsonCodec.FromJson(json);
            _splitCache[levelIndex] = CloneCupList(cups);
            return cups;
        }

        static void EnsureSplitIndex()
        {
            if (_splitIndexBuilt) return;
            _splitIndexBuilt = true;
            _splitLevelIndices = new HashSet<int>();

            if (Directory.Exists(ProjectPaths.LevelsSplitAbsolute))
            {
                foreach (var file in Directory.GetFiles(ProjectPaths.LevelsSplitAbsolute, "level_*.json"))
                    TryAddSplitIndex(file);
            }

#if UNITY_EDITOR
            if (_splitLevelIndices.Count == 0 && AssetDatabase.IsValidFolder(ProjectPaths.LevelsSplitAssetPath))
            {
                foreach (var guid in AssetDatabase.FindAssets("level_ t:TextAsset", new[] { ProjectPaths.LevelsSplitAssetPath }))
                    TryAddSplitIndex(AssetDatabase.GUIDToAssetPath(guid));
            }
#endif

            if (_splitLevelIndices.Count == 0)
            {
                var all = Resources.LoadAll<TextAsset>(ProjectPaths.LevelsSplitResourceFolder);
                foreach (var asset in all)
                    TryAddSplitIndex(asset.name);
            }

            if (_splitLevelIndices.Count > 0)
                Debug.Log($"[LevelConfigLoader] 关卡 JSON：{_splitLevelIndices.Count} 关 ({ProjectPaths.LevelsSplitAssetPath})");
        }

        static void TryAddSplitIndex(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("level_", StringComparison.OrdinalIgnoreCase)) return;
            if (int.TryParse(name.Substring("level_".Length), out var level))
                _splitLevelIndices.Add(level);
        }

        static string LoadSplitLevelJsonText(int levelIndex)
        {
            var diskPath = ProjectPaths.GetSplitLevelAbsolute(levelIndex);
            if (File.Exists(diskPath))
                return File.ReadAllText(diskPath);

#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ProjectPaths.GetSplitLevelAssetPath(levelIndex));
            if (asset != null && !string.IsNullOrEmpty(asset.text))
                return asset.text;
#endif

            var resource = Resources.Load<TextAsset>(ProjectPaths.GetSplitLevelResourcePath(levelIndex));
            return resource != null ? resource.text : null;
        }

        static string LoadConfTotalText()
        {
            if (File.Exists(ProjectPaths.LevelsConfTotalAbsolute))
                return File.ReadAllText(ProjectPaths.LevelsConfTotalAbsolute);

            var asset = Resources.Load<TextAsset>("Levels/ConfTotal");
            return asset != null ? asset.text : null;
        }

        public static Dictionary<string, List<List<object>>> ParseConfTotal(string json)
        {
            var result = new Dictionary<string, List<List<object>>>();
            if (string.IsNullOrWhiteSpace(json))
                return result;

            var root = JObject.Parse(json);
            foreach (var prop in root.Properties())
            {
                if (!prop.Name.StartsWith("level_", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (prop.Value is not JArray cups)
                    continue;

                var rawCups = new List<List<object>>();
                foreach (var cup in cups.OfType<JArray>())
                {
                    var entry = new List<object>();
                    foreach (var token in cup)
                        entry.Add(ToPlainObject(token));
                    rawCups.Add(entry);
                }

                result[prop.Name] = rawCups;
            }

            return result;
        }

        static object ToPlainObject(JToken token)
        {
            if (token is JObject obj)
            {
                var dict = new Dictionary<string, float>();
                foreach (var prop in obj.Properties())
                    dict[prop.Name] = prop.Value.Value<float>();
                return dict;
            }

            if (token is JArray arr)
                return arr.Select(t => t.Value<int>()).ToList();

            if (token is JValue v)
                return v.Value;

            return null;
        }

        public static List<CupData> MapCupData(List<List<object>> raw)
        {
            var result = new List<CupData>();
            if (raw == null)
                return result;

            for (var i = 0; i < raw.Count; i++)
            {
                var entry = raw[i];
                var pos = ReadPosition(entry.Count > 0 ? entry[0] : null);
                var colors = ReadIntList(entry.Count > 1 ? entry[1] : null);
                result.Add(new CupData
                {
                    id = i,
                    position = pos,
                    colors = colors,
                    whNums = ReadInt(entry, 2),
                    isVideo = ReadInt(entry, 3),
                    isLock = ReadInt(entry, 4),
                    lockColor = ReadInt(entry, 5),
                    lockNums = ReadInt(entry, 6),
                    isNull = ReadInt(entry, 7),
                    isEmptyCup = ReadInt(entry, 8)
                });
            }

            return result;
        }

        static Vector2 ReadPosition(object value)
        {
            if (value is Dictionary<string, float> pos)
                return new Vector2(pos.TryGetValue("x", out var x) ? x : 0f, pos.TryGetValue("y", out var y) ? y : 0f);
            return Vector2.zero;
        }

        static List<int> ReadIntList(object value)
        {
            if (value is List<int> ints)
                return new List<int>(ints);
            if (value is IEnumerable<object> objects)
                return objects.Select(Convert.ToInt32).ToList();
            return new List<int>();
        }

        static int ReadInt(List<object> entry, int index)
        {
            if (entry == null || index < 0 || index >= entry.Count || entry[index] == null)
                return 0;
            return Convert.ToInt32(entry[index]);
        }

        static int ParseLevelNumber(string key)
        {
            return key != null && key.StartsWith("level_", StringComparison.OrdinalIgnoreCase) &&
                   int.TryParse(key.Substring("level_".Length), out var n)
                ? n
                : 0;
        }

        static List<CupData> CloneCupList(List<CupData> source)
        {
            var clone = new List<CupData>();
            if (source == null)
                return clone;
            foreach (var cup in source)
                clone.Add(cup?.Clone());
            return clone;
        }
    }
}
