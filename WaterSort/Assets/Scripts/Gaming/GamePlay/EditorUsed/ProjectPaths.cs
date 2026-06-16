using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AsGame.Core
{
    /// <summary>项目路径约定。这里只保留当前项目仍使用的共享路径。</summary>
    public static class ProjectPaths
    {
        public const string LevelsSplitResourceFolder = "Levels/Split";

        public static string AssetBundleLocalJsonRootAbsolute =>
            Path.Combine(Application.dataPath, "AssetBundleLocal", "Json");

        public static string AssetBundleLocalJsonRootAssetPath =>
            "Assets/AssetBundleLocal/Json";

        public static string LevelsSplitAbsolute =>
            Path.Combine(AssetBundleLocalJsonRootAbsolute, "Levels");

        public static string LevelsSplitAssetPath =>
            $"{AssetBundleLocalJsonRootAssetPath}/Levels";

        public static string LevelsConfTotalAbsolute =>
            Path.Combine(LevelsSplitAbsolute, "ConfTotal.json");

        public static string ContentRootAbsolute =>
            Application.dataPath;

        public static string GetSplitLevelFileName(int levelIndex) =>
            $"level_{levelIndex}.json";

        public static string GetSplitLevelAbsolute(int levelIndex) =>
            Path.Combine(LevelsSplitAbsolute, GetSplitLevelFileName(levelIndex));

        public static string GetSplitLevelAssetPath(int levelIndex) =>
            $"{LevelsSplitAssetPath}/{GetSplitLevelFileName(levelIndex)}";

        public static string GetSplitLevelResourcePath(int levelIndex) =>
            $"{LevelsSplitResourceFolder}/level_{levelIndex}";

        public static IEnumerable<string> EnumerateResourcesRootsOnDisk()
        {
            if (!Directory.Exists(Application.dataPath))
                yield break;

            foreach (var dir in Directory.GetDirectories(Application.dataPath, "Resources", SearchOption.AllDirectories))
                yield return dir;
        }
    }
}
