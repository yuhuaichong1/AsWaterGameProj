using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    [Serializable]
    public class LevelDesignSheetEntry
    {
        public int level;
        public int regular;
        public int empty;
        public int lockCup;
        public int lockLayers;
        public int ad;
        public int slot;
        public int colors;
        public int layers;
    }

    [Serializable]
    class LevelDesignSheetDocument
    {
        public LevelDesignSheetEntry[] entries;
    }

    /// <summary>读取「优化版v1」导出的 LevelDesignV1.json。</summary>
    public static class LevelDesignSheetLoader
    {
        const string JsonAssetPath = "Assets/Editor/LevelEditor/LevelDesignV1.json";

        static Dictionary<int, LevelDesignSheetEntry> _cache;

        public static bool TryGetEntry(int level, out LevelDesignSheetEntry entry)
        {
            EnsureLoaded();
            return _cache.TryGetValue(level, out entry);
        }

        public static IReadOnlyList<LevelDesignSheetEntry> GetAll()
        {
            EnsureLoaded();
            var list = new List<LevelDesignSheetEntry>(_cache.Values);
            list.Sort((a, b) => a.level.CompareTo(b.level));
            return list;
        }

        public static int TryGetExpectedLockLayers(int level)
        {
            if (!TryGetEntry(level, out var entry) || entry.lockCup <= 0)
                return -1;
            return entry.lockLayers;
        }

        static void EnsureLoaded()
        {
            if (_cache != null) return;
            _cache = new Dictionary<int, LevelDesignSheetEntry>();

            var abs = Path.Combine(Application.dataPath, "Editor/LevelEditor/LevelDesignV1.json");
            if (!File.Exists(abs))
            {
                Debug.LogWarning($"[LevelDesignSheetLoader] 未找到 {JsonAssetPath}");
                return;
            }

            var json = File.ReadAllText(abs);
            var wrapped = "{\"entries\":" + json + "}";
            var doc = JsonUtility.FromJson<LevelDesignSheetDocument>(wrapped);
            if (doc?.entries == null) return;

            foreach (var e in doc.entries)
                _cache[e.level] = e;
        }

        public static void InvalidateCache() => _cache = null;
    }
}
