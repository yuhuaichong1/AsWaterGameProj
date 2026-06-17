/**
 * AssetBundle打包构建
 */

using System;
using System.IO;
using UnityEditor;
using System.Collections.Generic;
using XrCode;

namespace Assets.Editor.AssetBundle
{
    public class AssetBundleBuilder
    {
        /// <summary>
        /// AssetBundle文件后缀
        /// </summary>
        public const string ASSETBUNDLEEX = ".assetbundle";
        /// <summary>
        /// 资源依赖映射
        /// </summary>
        private static Dictionary<string, AssetItem> mAssetItemDict = new Dictionary<string, AssetItem>();
        /// <summary>
        /// SpriteAtlas依赖映射
        /// </summary>
        private static Dictionary<string, string> mSpriteAtlasDict = new Dictionary<string, string>();

        [MenuItem("Tools/AssetBundle/Build", false, 1)]
        public static void Build()
        {
            ExecuteBuild(fullBuild: true);
        }

        [MenuItem("Tools/AssetBundle/Build Incremental", false, 2)]
        public static void BuildIncremental()
        {
            if (!IncrementalBuildCache.CanIncrementalBuild())
            {
                D.Error("[AssetBundle] Incremental build requires a previous successful full build. Please run Tools/AssetBundle/Build first.");
                return;
            }

            ExecuteBuild(fullBuild: false);
        }

        [MenuItem("Tools/AssetBundle/Build Incremental", true)]
        private static bool ValidateBuildIncremental()
        {
            return IncrementalBuildCache.CanIncrementalBuild();
        }

        private static void ExecuteBuild(bool fullBuild)
        {
            mSpriteAtlasDict.Clear();
            mAssetItemDict.Clear();
            ClearAssetBundleNames();

            var buildSucceeded = false;
            try
            {
                //CreateLuaBytes();
                CreateSpriteAtlasMap();
                CreateAssetDependsMap();
                GroupAssetBundles();

                if (!fullBuild)
                {
                    var dirtyBundles = IncrementalBuildCache.GetDirtyBundleNames(mAssetItemDict);
                    if (dirtyBundles.Count == 0)
                    {
                        D.Log("[AssetBundle] Incremental build: no changes detected, skipped.");
                        buildSucceeded = true;
                        return;
                    }

                    D.Log("[AssetBundle] Incremental build: {0} bundle(s) need rebuild.", dirtyBundles.Count);
                }

                CreateAssetBundleConfig();
                SetAssetBundleNames();
                AssetDatabase.SaveAssets();
                BuildAssetBundles(fullBuild);
                IncrementalBuildCache.Save(mAssetItemDict);
                buildSucceeded = true;

                if (fullBuild)
                {
                    D.Log("AssetBundle Build Success!");
                }
                else
                {
                    D.Log("AssetBundle Incremental Build Success!");
                }
            }
            catch (Exception ex)
            {
                D.Error("[AssetBundle] Build failed: {0}", ex.Message);
                throw;
            }
            finally
            {
                ClearAssetBundleNames();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();

                if (!buildSucceeded)
                {
                    D.Log("[AssetBundle] Build cache was not updated due to failure.");
                }
            }
        }

        [MenuItem("Tools/AssetBundle/Clear All AssetBundle Names", false, 3)]
        public static void ClearAllAssetBundleNamesMenu()
        {
            ClearAssetBundleNames();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            D.Log("[AssetBundle] All asset bundle names cleared.");
        }

        /// <summary>
        /// 是否应纳入 AB 打包（仅限 AssetBundleLocal 内、非 Editor 目录的运行时资源）
        /// </summary>
        private static bool ShouldBundleAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            path = path.Replace("\\", "/");
            if (!AssetUtils.ValidAsset(path))
            {
                return false;
            }

            if (AssetUtils.IsEditorAssetPath(path))
            {
                return false;
            }

            return path.StartsWith(BuilderConfig.AssetRootPath);
        }

        /// <summary>
        /// 创建Lua二进制文件
        /// Assets/LuaScripts/*.lua -> Assets/AssetBundleLocal/LuaScripts/*.lua.bytes
        /// </summary>
        public static void CreateLuaBytes()
        {
            if (Directory.Exists(BuilderConfig.LuaScriptsDestPath))
            {
                UnityEditor.FileUtil.DeleteFileOrDirectory(BuilderConfig.LuaScriptsDestPath);
            }
            var dir = new DirectoryInfo(BuilderConfig.LuaScriptsSrcPath);
            if (!dir.Exists) return;
            var stack = new Stack<DirectoryInfo>();
            stack.Push(dir);
            while (stack.Count > 0)
            {
                var dirInfo = stack.Pop();
                var files = dirInfo.GetFiles();

                foreach (var file in files)
                {
                    if (AssetUtils.ValidAsset(file.FullName))
                    {
                        var fullPath = file.FullName.Replace("\\", "/");
                        var startIndex = fullPath.IndexOf("LuaScripts");
                        var destPath = fullPath.Insert(startIndex, "AssetBundleLocal/") + ".bytes";
                        if (XrCode.FileUtil.CheckFileAndCreateDirWhenNeeded(destPath))
                        {
                            file.CopyTo(destPath);
                        }
                    }
                }

                var subDirs = dirInfo.GetDirectories();
                foreach (var subDir in subDirs)
                {
                    stack.Push(subDir);
                }
            }
        }

        /// <summary>
        /// 遍历Atlas下所有的SpriteAtlas，建立图片资源依赖
        /// </summary>
        public static void CreateSpriteAtlasMap()
        {
            var dir = new DirectoryInfo(BuilderConfig.SpriteAtlasPath);
            if (!dir.Exists) return;
            var files = dir.GetFiles();
            for (var i = 0; i < files.Length; i++)
            {
                var fileInfo = files[i];
                EditorUtility.DisplayProgressBar("CreateSpriteAtlasMap", fileInfo.FullName, 1.0f * (i + 1) / files.Length);
                if (!AssetUtils.ValidAsset(fileInfo.FullName))
                {
                    continue;
                }

                var fullPath = fileInfo.FullName.Replace("\\", "/");
                var path = fullPath.Substring(fullPath.IndexOf(BuilderConfig.AssetRootPath));
                if (!ShouldBundleAsset(path))
                {
                    continue;
                }

                var assetItem = GetAssetItem(path);
                if (assetItem == null)
                {
                    continue;
                }

                var depends = AssetDatabase.GetDependencies(path);
                foreach (var depend in depends)
                {
                    if (!ShouldBundleAsset(depend) || depend == path)
                    {
                        continue;
                    }

                    if (!mSpriteAtlasDict.ContainsKey(depend))
                    {
                        mSpriteAtlasDict.Add(depend, path);
                    }

                    if (!assetItem.Depends.Contains(depend))
                    {
                        assetItem.Depends.Add(depend);
                    }

                    var dependAssetItem = GetAssetItem(depend);
                    if (dependAssetItem != null && !dependAssetItem.BeDepends.Contains(path))
                    {
                        dependAssetItem.BeDepends.Add(path);
                    }
                }

                if (!mSpriteAtlasDict.ContainsKey(path))
                {
                    mSpriteAtlasDict.Add(path, path);
                }
            }
        }

        /// <summary>
        /// 遍历资源，建立依赖映射
        /// </summary>
        public static void CreateAssetDependsMap()
        {
            var stack = new Stack<DirectoryInfo>();
            stack.Push(new DirectoryInfo(BuilderConfig.AssetRootPath));
            while (stack.Count > 0)
            {
                var dirInfo = stack.Pop();
                var childDirs = dirInfo.GetDirectories();
                if (childDirs != null)
                {
                    foreach (var dir in childDirs)
                    {
                        stack.Push(dir);
                    }
                }
                var files = dirInfo.GetFiles();
                for (var i = 0; i < files.Length; i++)
                {
                    var fileInfo = files[i];
                    EditorUtility.DisplayProgressBar("CreateAssetDependsMap", fileInfo.FullName, 1.0f * (i + 1) / files.Length);
                    if (!AssetUtils.ValidAsset(fileInfo.FullName))
                    {
                        continue;
                    }

                    var fullPath = fileInfo.FullName.Replace("\\", "/");
                    var path = fullPath.Substring(fullPath.IndexOf(BuilderConfig.AssetRootPath));
                    if (!ShouldBundleAsset(path))
                    {
                        continue;
                    }

                    // 过滤掉在图集中的资源
                    if (mSpriteAtlasDict.ContainsKey(path))
                    {
                        continue;
                    }

                    var assetItem = GetAssetItem(path);
                    if (assetItem == null)
                    {
                        continue;
                    }

                    var depends = AssetDatabase.GetDependencies(path);
                    foreach (var depend in depends)
                    {
                        if (!ShouldBundleAsset(depend) || depend == path)
                        {
                            continue;
                        }

                        // 如果依赖的Sprite有对应的图集，就改为依赖SpriteAtlas
                        var dependPath = mSpriteAtlasDict.ContainsKey(depend) ? mSpriteAtlasDict[depend] : depend;
                        if (!ShouldBundleAsset(dependPath))
                        {
                            continue;
                        }

                        if (assetItem.Depends.Contains(dependPath))
                        {
                            continue;
                        }

                        assetItem.Depends.Add(dependPath);
                        var dependAssetItem = GetAssetItem(dependPath);
                        if (dependAssetItem != null && !dependAssetItem.BeDepends.Contains(path))
                        {
                            dependAssetItem.BeDepends.Add(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获得资源信息
        /// </summary>
        private static AssetItem GetAssetItem(string path)
        {
            if (!ShouldBundleAsset(path))
            {
                return null;
            }

            AssetItem item;
            if (!mAssetItemDict.TryGetValue(path, out item))
            {
                item = new AssetItem();
                item.AssetBundleName = GetAssetBundleName(path);
                mAssetItemDict.Add(path, item);
            }
            return item;
        }

        /// <summary>
        /// 合并依赖，并分组资源
        /// </summary>
        public static void GroupAssetBundles()
        {
            /* 清除同层依赖 把同层之间被依赖的节点下移 (a->b, a->c, b->c) ==> (a->b->c)
             *      a              a
             *     /  \    ==>    /
             *    b -> c         b
             *                  /
             *                 c 
             *  例如：prefab上挂着mat, mat依赖shder。特别注意，此时prefab同时依赖mat,和shader。可以点击右键查看
             *  (prefab->mat, prefab->shader, mat->shader) ==> (prefab->mat->shader)
             */
            var removeList = new List<string>();
            foreach (var item in mAssetItemDict)
            {
                removeList.Clear();
                var path = item.Key;
                var assetItem = item.Value;
                foreach (var depend in assetItem.Depends)
                {
                    AssetItem dependAssetItem;
                    if (!mAssetItemDict.TryGetValue(depend, out dependAssetItem))
                    {
                        continue;
                    }

                    foreach (var beDepend in dependAssetItem.BeDepends)
                    {
                        if (assetItem.Depends.Contains(beDepend))
                        {
                            removeList.Add(depend);
                        }
                    }
                }
                foreach (var depend in removeList)
                {
                    assetItem.Depends.Remove(depend);
                    AssetItem dependAssetItem;
                    if (mAssetItemDict.TryGetValue(depend, out dependAssetItem))
                    {
                        dependAssetItem.BeDepends.Remove(path);
                    }
                }
            }

            /* 向上归并依赖
             *      a        e                 
             *       \      /                    
             *        b    f     ==>  (a,b,c,h) -> (d) <- (e,f)
             *      / | \ /                          
             *     c  h  d      
             */
            foreach (var item in mAssetItemDict)
            {
                var path = item.Key;
                var assetItem = item.Value;

                while (assetItem.BeDepends.Count == 1)
                {
                    AssetItem parentItem;
                    if (!mAssetItemDict.TryGetValue(assetItem.BeDepends[0], out parentItem))
                    {
                        break;
                    }

                    assetItem = parentItem;
                    var isCompa = assetItem.BeDepends.Contains(path);
                    if (assetItem.BeDepends.Count != 1 || isCompa)
                    {
                        D.Log(isCompa, $"[ABMod]: {assetItem.AssetBundleName} ___ {path}");
                        if (isCompa)
                        {
                            break;
                        }

                        item.Value.AssetBundleName = assetItem.AssetBundleName;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 生成AssetBundleConfig配置文件
        /// </summary>
        private static void CreateAssetBundleConfig()
        {
            var configPath = BuilderConfig.PathBundleConfig;
            var configBundleName = GetAssetBundleName(configPath);
            var config = new PathBundleInfoList();
            config.List.Add(new PathBundleInfo() { Path = configPath, AssetBundleName = configBundleName });
            foreach (var item in mAssetItemDict)
            {
                if (item.Key != configPath)
                {
                    var pathBundleInfo = new PathBundleInfo();
                    pathBundleInfo.Path = item.Key;
                    pathBundleInfo.AssetBundleName = item.Value.AssetBundleName;
                    config.List.Add(pathBundleInfo);
                }
            }
            var buffer = ProtobufUtil.NSerialize(config);
            XrCode.FileUtil.WriteAllBytes(configPath, buffer);
            AssetDatabase.ImportAsset(configPath);

            var assetItem = GetAssetItem(configPath);
            if (assetItem != null)
            {
                assetItem.AssetBundleName = configBundleName;
            }
        }

        /// <summary>
        /// 设置所有的ABName
        /// </summary>
        private static void SetAssetBundleNames()
        {
            foreach (var item in mAssetItemDict)
            {
                var path = item.Key;
                if (!ShouldBundleAsset(path))
                {
                    continue;
                }

                var assetItem = item.Value;
                var assetImport = AssetImporter.GetAtPath(path);
                if (assetImport != null)
                {
                    assetImport.assetBundleName = assetItem.AssetBundleName.ToLower();
                }
            }
        }

        /// <summary>
        /// 获取完整AssetBundle包名
        /// </summary>
        private static string GetAssetBundleName(string path)
        {
            return (path + ASSETBUNDLEEX).ToLower();
        }

        /// <summary>
        /// 清除所有ABName
        /// </summary>
        private static void ClearAssetBundleNames()
        {
            var abNames = AssetDatabase.GetAllAssetBundleNames();
            for (var i = 0; i < abNames.Length; i++)
            {
                AssetDatabase.RemoveAssetBundleName(abNames[i], true);
            }
        }

        /// <summary>
        /// 生成AssetBundles
        /// </summary>
        /// <param name="fullBuild">true=全量（清空输出目录）；false=增量（保留已有 AB，由 Unity 跳过未变更包）</param>
        private static void BuildAssetBundles(bool fullBuild)
        {
            EditorUtility.DisplayProgressBar("BuildAssetBundles", fullBuild ? "Full Build" : "Incremental Build", 0);
            if (fullBuild)
            {
                if (Directory.Exists(BuilderConfig.AssetBundleExportPath))
                {
                    UnityEditor.FileUtil.DeleteFileOrDirectory(BuilderConfig.AssetBundleExportPath);
                }
            }

            if (!Directory.Exists(BuilderConfig.AssetBundleExportPath))
            {
                Directory.CreateDirectory(BuilderConfig.AssetBundleExportPath);
            }

            BuildPipeline.BuildAssetBundles(
                BuilderConfig.AssetBundleExportPath,
                BuilderConfig.Options,
                EditorUserBuildSettings.activeBuildTarget);
        }
    }
}
