using System;
using System.Collections.Generic;
using System.IO;
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
        const string ResourcePath = "Levels/ConfTotal";

        static TextAsset _cachedAsset;
        static Dictionary<string, List<List<object>>> _rawLevels;

        public static List<CupData> LoadLevel(int levelIndex)
        {
            EnsureLoaded();
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

        public static int GetLevelCount()
        {
            EnsureLoaded();
            return _rawLevels?.Count ?? 0;
        }

        static void EnsureLoaded()
        {
            if (_rawLevels != null) return;

            var json = LoadJsonText();
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError(
                    "[LevelConfigLoader] 未找到关卡配置。请确认存在：\n" +
                    ProjectPaths.ToEditorAssetPath("Levels/ConfTotal.json") + "\n" +
                    "且 .meta 为 TextScriptImporter（勿使用 Cocos 的 json meta）。");
                _rawLevels = new Dictionary<string, List<List<object>>>();
                return;
            }

            _rawLevels = ParseConfTotal(json);
            if (_rawLevels.Count == 0)
                Debug.LogWarning("[LevelConfigLoader] ConfTotal 解析到 0 关，请检查 JSON 格式。");
            else
                Debug.Log($"[LevelConfigLoader] 已加载 {_rawLevels.Count} 关");
        }

        static string LoadJsonText()
        {
            _cachedAsset = Resources.Load<TextAsset>(ResourcePath);
            if (_cachedAsset != null && !string.IsNullOrEmpty(_cachedAsset.text))
                return _cachedAsset.text;

            // 部分工程首次导入前 Resources 索引未就绪，尝试扫描 Levels 目录
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
            var editorJson = TryLoadJsonFromAssetDatabase();
            if (!string.IsNullOrEmpty(editorJson))
                return editorJson;
#endif

            foreach (var path in GetDiskCandidates())
            {
                if (!File.Exists(path)) continue;
                try
                {
                    if (Application.isEditor)
                        Debug.Log(
                            "[LevelConfigLoader] 使用磁盘关卡配置（编辑器；发布包依赖 Resources.Load）: " +
                            path);
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
        static string TryLoadJsonFromAssetDatabase()
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

        static IEnumerable<string> GetDiskCandidates()
        {
            foreach (var resourcesRoot in ProjectPaths.EnumerateResourcesRootsOnDisk())
                yield return Path.Combine(resourcesRoot, "Levels", "ConfTotal.json");

            yield return ProjectPaths.LevelsConfTotalAbsolute;
            yield return Path.Combine(Application.streamingAssetsPath, "Levels", "ConfTotal.json");
        }

        /// <summary>
        /// 解析 Cocos ConfTotal.json：level_N -> [[pos, colors, wh, video, lock, lockColor, lockNums], ...]
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
                    isNull = entry.Count > 7 ? Convert.ToInt32(entry[7]) : 0
                });
            }

            return list;
        }
    }
}
