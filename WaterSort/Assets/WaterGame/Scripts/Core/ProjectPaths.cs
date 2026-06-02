using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AsGame.Core
{
    /// <summary>
    /// 工程内容根目录（Assets/WaterGame）下的路径约定。
    /// Resources.Load 仍使用相对 Resources 的路径（如 Levels/ConfTotal），与文件夹在 Assets 下的位置无关。
    /// </summary>
    public static class ProjectPaths
    {
        public const string ContentFolderName = "WaterGame";

        public static string ContentRootAbsolute =>
            Path.Combine(Application.dataPath, ContentFolderName);

        public static string ResourcesRootAbsolute =>
            Path.Combine(ContentRootAbsolute, "Resources");

        /// <summary>Unity 工程内路径，例如 Assets/WaterGame/Resources。</summary>
        public static string ResourcesRootAssetPath =>
            $"Assets/{ContentFolderName}/Resources";

        public static string PrefabsRootAssetPath =>
            $"{ResourcesRootAssetPath}/Prefabs";

        public static string LevelsConfTotalAbsolute =>
            Path.Combine(ResourcesRootAbsolute, "Levels", "ConfTotal.json");

        public static string ToEditorAssetPath(string pathUnderResources) =>
            $"{ResourcesRootAssetPath}/{pathUnderResources.TrimStart('/', '\\')}";

        /// <summary>磁盘上所有名为 Resources 的目录（优先 WaterGame/Resources）。</summary>
        public static IEnumerable<string> EnumerateResourcesRootsOnDisk()
        {
            var primary = ResourcesRootAbsolute;
            if (Directory.Exists(primary))
                yield return primary;

            if (!Directory.Exists(Application.dataPath))
                yield break;

            foreach (var dir in Directory.GetDirectories(Application.dataPath, "Resources", SearchOption.AllDirectories))
            {
                if (string.Equals(dir, primary, StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return dir;
            }
        }
    }
}
