#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

namespace AsGame.Editor
{
    /// <summary>将 Resources/Spine 下 3.8 JSON 升级为 4.2，并刷新 SkeletonData 引用。</summary>
    public static class SpineJsonUpgradeUtility
    {
        const string SpineRoot = "Assets/WaterGame/Resources/Spine";
        const string ConverterExe = "Assets/WaterGame/Editor/Tools/SpineSkeletonDataConverter.exe";
        const string TargetVersion = "4.2.11";

        [MenuItem("AsGame/Spine/Upgrade JSON To 4.2 And Refresh Assets", false, 70)]
        public static void UpgradeAllAndRefresh()
        {
            if (!File.Exists(ConverterExe))
            {
                EditorUtility.DisplayDialog("Spine 升级",
                    "未找到转换工具:\n" + ConverterExe + "\n请从 _tools 目录复制 SpineSkeletonDataConverter.exe 到 Editor/Tools。",
                    "OK");
                return;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var converterPath = Path.Combine(projectRoot, ConverterExe.Replace('/', Path.DirectorySeparatorChar));
            var tmpRoot = Path.Combine(projectRoot, "Library", "Spine42Upgrade");
            if (Directory.Exists(tmpRoot))
                Directory.Delete(tmpRoot, true);
            Directory.CreateDirectory(tmpRoot);

            var jsonFiles = Directory.GetFiles(
                Path.Combine(projectRoot, SpineRoot.Replace('/', Path.DirectorySeparatorChar)),
                "*.json",
                SearchOption.AllDirectories);

            var upgraded = 0;
            foreach (var jsonPath in jsonFiles)
            {
                var rel = jsonPath.Substring(projectRoot.Length + 1);
                var outPath = Path.Combine(tmpRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

                var psi = new ProcessStartInfo
                {
                    FileName = converterPath,
                    Arguments = $"\"{jsonPath}\" \"{outPath}\" -v {TargetVersion}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var proc = Process.Start(psi);
                proc!.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"[SpineJsonUpgrade] 失败: {rel}\n{proc.StandardError.ReadToEnd()}");
                    continue;
                }

                File.Copy(outPath, jsonPath, true);
                upgraded++;
            }

            LinkAtlasAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Spine 升级",
                $"已升级 {upgraded} 个 JSON 到 Spine {TargetVersion}，并刷新 SkeletonData 图集引用。",
                "OK");
        }

        [MenuItem("AsGame/Spine/Refresh SkeletonData Atlas Links", false, 71)]
        public static void LinkAtlasAssetsMenu()
        {
            LinkAtlasAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Spine", "已刷新 SkeletonData 图集引用。", "OK");
        }

        static void LinkAtlasAssets()
        {
            foreach (var skeletonGuid in AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { SpineRoot }))
            {
                var skeletonPath = AssetDatabase.GUIDToAssetPath(skeletonGuid);
                var skeleton = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonPath);
                if (skeleton == null) continue;

                var dir = Path.GetDirectoryName(skeletonPath)!.Replace('\\', '/');
                var atlasGuids = AssetDatabase.FindAssets("t:SpineAtlasAsset", new[] { dir });
                if (atlasGuids.Length == 0) continue;

                var atlas = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(
                    AssetDatabase.GUIDToAssetPath(atlasGuids[0]));
                if (atlas == null) continue;

                skeleton.atlasAssets = new AtlasAssetBase[] { atlas };
                skeleton.Clear();
                EditorUtility.SetDirty(skeleton);
            }
        }
    }
}
#endif
