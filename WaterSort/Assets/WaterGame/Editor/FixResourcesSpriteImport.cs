#if UNITY_EDITOR
using System.IO;
using System.Text;
using AsGame.Core;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor
{
    public static class FixResourcesSpriteImport
    {
        static readonly string[] ProbePaths =
        {
            "Sprites/Bottle/img_2",
            "Sprites/Bottle/rect",
            "Sprites/Bottle/sgsg",
            "Sprites/Pocket/1_1",
            "Sprites/Home/logo_home",
            "Sprites/Home/bg_home",
            "Sprites/UI/btn_main",
            "Sprites/Loading/bj2",
        };

        [MenuItem("AsGame/Fix Resources Sprite Import (修复贴图导入)", false, 60)]
        public static void FixAll()
        {
            var report = FixInternal(forceReimportAll: false);
            EditorUtility.DisplayDialog("贴图导入修复", report, "确定");
        }

        [MenuItem("AsGame/Force Reimport All Resources PNG (强制重导)", false, 61)]
        public static void ForceReimportAll()
        {
            if (!EditorUtility.DisplayDialog("强制重导",
                    $"将强制重新导入 {ProjectPaths.ResourcesRootAssetPath} 下全部 PNG/JPG（即使设置已正确）。\n用于 Library 缓存异常时。",
                    "继续", "取消"))
                return;

            var report = FixInternal(forceReimportAll: true);
            EditorUtility.DisplayDialog("强制重导完成", report, "确定");
        }

        [MenuItem("AsGame/Diagnose Resources Sprites (诊断贴图加载)", false, 62)]
        public static void Diagnose()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Resources 贴图加载诊断：");
            sb.AppendLine("(Sprite 2D and UI = Unity 里的 UI Sprite 格式)");
            sb.AppendLine();

            foreach (var path in ProbePaths)
            {
                var sprite = Resources.Load<Sprite>(path);
                var tex = Resources.Load<Texture2D>(path);
                var diskBase = ProjectPaths.ResourcesRootAbsolute;
                var assetPath = ProjectPaths.ToEditorAssetPath(path) +
                                (File.Exists(Path.Combine(diskBase, path + ".png")) ? ".png" : ".jpg");
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                var typeStr = importer != null ? importer.textureType.ToString() : "无 importer";

                sb.AppendLine($"{path}:");
                sb.AppendLine($"  Importer: {typeStr}");
                sb.AppendLine($"  Load<Sprite>: {(sprite != null ? "OK" : "失败")}");
                sb.AppendLine($"  Load<Texture2D>: {(tex != null ? "OK" : "失败")}");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("诊断结果", sb.ToString(), "确定");
        }

        static string FixInternal(bool forceReimportAll)
        {
            var root = ProjectPaths.ResourcesRootAbsolute;
            if (!Directory.Exists(root))
                return $"未找到 {ProjectPaths.ResourcesRootAssetPath} 目录。";

            var scanned = 0;
            var alreadyOk = 0;
            var fixedCount = 0;
            var forced = 0;

            foreach (var abs in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(abs).ToLowerInvariant();
                if (ext != ".png" && ext != ".jpg") continue;

                var assetPath = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;

                scanned++;
                var dirty = forceReimportAll;

                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    if (forceReimportAll) forced++;
                    else fixedCount++;
                }
                else
                {
                    alreadyOk++;
                }
            }

            AssetDatabase.Refresh();

            var sb = new StringBuilder();
            sb.AppendLine($"扫描: {scanned} 张 PNG/JPG");
            sb.AppendLine($"已是 Sprite(2D and UI): {alreadyOk} 张");
            if (forceReimportAll)
                sb.AppendLine($"强制重导: {forced} 张");
            else
                sb.AppendLine($"本次修正并重导: {fixedCount} 张");

            if (!forceReimportAll && fixedCount == 0)
            {
                sb.AppendLine();
                sb.AppendLine("0 张通常表示：导入类型已是 Sprite(2D and UI)，无需再改。");
                sb.AppendLine("若画面仍无色块贴图，请点：");
                sb.AppendLine("AsGame → Diagnose Resources Sprites");
                sb.AppendLine("或 Force Reimport All Resources PNG");
            }

            sb.AppendLine();
            sb.AppendLine("请重新 Play 查看效果。");
            return sb.ToString();
        }
    }
}
#endif
