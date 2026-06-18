/**
 * 增量 AB 构建缓存：记录上次成功构建时各资源的依赖哈希与包名映射
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.AssetBundle
{
    [Serializable]
    public class AssetBundleBuildCacheFile
    {
        public List<AssetBundleBuildCacheEntry> Entries = new List<AssetBundleBuildCacheEntry>();
    }

    [Serializable]
    public class AssetBundleBuildCacheEntry
    {
        public string Path;
        public string BundleName;
        public string DependencyHash;
    }

    public static class IncrementalBuildCache
    {
        private static string CacheFilePath
        {
            get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "AssetBundleBuildCache.json"); }
        }

        public static bool CanIncrementalBuild()
        {
            if (!File.Exists(CacheFilePath))
            {
                return false;
            }

            if (!Directory.Exists(BuilderConfig.AssetBundleExportPath))
            {
                return false;
            }

            var manifestPath = Path.Combine(
                BuilderConfig.AssetBundleExportPath,
                EditorUserBuildSettings.activeBuildTarget.ToString());
            return File.Exists(manifestPath);
        }

        public static Dictionary<string, AssetBundleBuildCacheEntry> Load()
        {
            var result = new Dictionary<string, AssetBundleBuildCacheEntry>();
            if (!File.Exists(CacheFilePath))
            {
                return result;
            }

            try
            {
                var json = File.ReadAllText(CacheFilePath);
                var cacheFile = JsonUtility.FromJson<AssetBundleBuildCacheFile>(json);
                if (cacheFile?.Entries == null)
                {
                    return result;
                }

                foreach (var entry in cacheFile.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Path))
                    {
                        continue;
                    }

                    result[entry.Path] = entry;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AssetBundle] Failed to load incremental cache: " + ex.Message);
            }

            return result;
        }

        public static void Save(Dictionary<string, AssetItem> assetItemDict)
        {
            var cacheFile = new AssetBundleBuildCacheFile();
            foreach (var item in assetItemDict)
            {
                cacheFile.Entries.Add(new AssetBundleBuildCacheEntry
                {
                    Path = item.Key,
                    BundleName = item.Value.AssetBundleName,
                    DependencyHash = GetDependencyHash(item.Key)
                });
            }

            var cacheDir = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(cacheDir) && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var json = JsonUtility.ToJson(cacheFile, true);
            File.WriteAllText(CacheFilePath, json);
        }

        public static string GetDependencyHash(string assetPath)
        {
            return AssetDatabase.GetAssetDependencyHash(assetPath).ToString();
        }

        /// <summary>
        /// 对比当前分组结果与缓存，返回需要重新构建的 AB 包名集合
        /// </summary>
        public static HashSet<string> GetDirtyBundleNames(Dictionary<string, AssetItem> assetItemDict)
        {
            var dirtyBundles = new HashSet<string>();
            var oldCache = Load();

            foreach (var item in assetItemDict)
            {
                var path = item.Key;
                var bundleName = item.Value.AssetBundleName;
                var hash = GetDependencyHash(path);

                AssetBundleBuildCacheEntry oldEntry;
                if (!oldCache.TryGetValue(path, out oldEntry)
                    || oldEntry.DependencyHash != hash
                    || oldEntry.BundleName != bundleName)
                {
                    dirtyBundles.Add(bundleName);
                    if (oldEntry != null && !string.IsNullOrEmpty(oldEntry.BundleName))
                    {
                        dirtyBundles.Add(oldEntry.BundleName);
                    }
                }
            }

            foreach (var oldEntry in oldCache.Values)
            {
                if (!assetItemDict.ContainsKey(oldEntry.Path)
                    && !string.IsNullOrEmpty(oldEntry.BundleName))
                {
                    dirtyBundles.Add(oldEntry.BundleName);
                }
            }

            if (dirtyBundles.Count > 0)
            {
                dirtyBundles.Add(GetAssetBundleName(BuilderConfig.PathBundleConfig));
            }

            return dirtyBundles;
        }

        private static string GetAssetBundleName(string path)
        {
            return (path + AssetBundleBuilder.ASSETBUNDLEEX).ToLower();
        }
    }
}
