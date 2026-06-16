using System;
using System.Collections.Generic;
using UnityEngine;
using XrCode;

namespace AsGame.Data
{
    [Serializable]
    public class SplitLevelDocument
    {
        public int level;
        public CupJsonEntry[] cups = Array.Empty<CupJsonEntry>();
    }

    [Serializable]
    public class CupJsonEntry
    {
        public float x;
        public float y;
        public int[] colors = Array.Empty<int>();
        public int whNums;
        public int isVideo;
        public int isLock;
        public int lockColor;
        public int lockNums;
        public int isNull;
        public int isEmptyCup;
    }

    public static class LevelJsonCodec
    {
        public static string ToJson(int levelIndex, IReadOnlyList<CupData> cups, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(new SplitLevelDocument
            {
                level = levelIndex,
                cups = ToEntries(cups)
            }, prettyPrint);
        }

        public static List<CupData> FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<CupData>();

            var doc = JsonUtility.FromJson<SplitLevelDocument>(json);
            return FromEntries(doc?.cups);
        }

        static CupJsonEntry[] ToEntries(IReadOnlyList<CupData> cups)
        {
            if (cups == null || cups.Count == 0)
                return Array.Empty<CupJsonEntry>();

            var entries = new CupJsonEntry[cups.Count];
            for (var i = 0; i < cups.Count; i++)
            {
                var c = cups[i];
                entries[i] = new CupJsonEntry
                {
                    x = c.position.x,
                    y = c.position.y,
                    colors = c.colors != null ? c.colors.ToArray() : Array.Empty<int>(),
                    whNums = c.whNums,
                    isVideo = c.isVideo,
                    isLock = c.isLock,
                    lockColor = c.lockColor,
                    lockNums = c.lockNums,
                    isNull = c.isNull,
                    isEmptyCup = c.isEmptyCup
                };
            }

            return entries;
        }

        static List<CupData> FromEntries(CupJsonEntry[] entries)
        {
            var list = new List<CupData>();
            if (entries == null)
                return list;

            for (var i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                list.Add(new CupData
                {
                    id = i,
                    position = new Vector2(e.x, e.y),
                    colors = e.colors != null ? new List<int>(e.colors) : new List<int>(),
                    whNums = e.whNums,
                    isVideo = e.isVideo,
                    isLock = e.isLock,
                    lockColor = e.lockColor,
                    lockNums = e.lockNums,
                    isNull = e.isNull,
                    isEmptyCup = e.isEmptyCup
                });
            }

            return list;
        }
    }
}
