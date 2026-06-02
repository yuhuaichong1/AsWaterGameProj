#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AsGame.UI.Popups;

namespace AsGame.Editor.CocosImport
{
    public static class CocosPrefabImporter
    {
        static readonly Dictionary<string, Type> ScriptByPrefabName = new()
        {
            { "SettingPopup", typeof(SettingPopupView) },
            { "SuccessPopup", typeof(SuccessPopupView) },
            { "RankPopup", typeof(RankPopupView) },
            { "CollectPopup", typeof(CollectPopupView) },
            { "RecoverHeartPopup", typeof(RecoverHeartPopupView) },
            { "GetCollectPopup", typeof(GetCollectPopupView) },
            { "GetHeartPopup", typeof(GetHeartPopupView) },
            { "DailyHeartPopup", typeof(DailyHeartPopupView) },
            { "FeedbackPopup", typeof(FeedbackPopupView) },
            { "NewPlayPopup", typeof(NewPlayPopupView) },
        };

        [MenuItem("AsGame/Cocos Import/★ 一键导入全部弹窗 Prefab", false, 0)]
        public static void ImportAllPopupsOneClick()
        {
            if (!EditorUtility.DisplayDialog("批量导入 Cocos 弹窗",
                    "将从 Cocos 工程读取全部弹窗 Prefab（已排除微信/抖音/快手），\n" +
                    "生成 Unity Prefab + 总对照表。\n\n预计 1～3 分钟，是否继续？",
                    "开始导入", "取消"))
                return;

            var results = ImportAllPopups(showDialogs: false);
            WriteMasterReport(results);
            AssetDatabase.Refresh();

            var ok = results.Count(r => r.Success);
            var fail = results.Count - ok;
            EditorUtility.DisplayDialog("批量导入完成",
                $"成功: {ok}\n失败: {fail}\n\n" +
                $"Prefab 目录:\n{CocosImportSettings.UnityImportRoot}/UI\n\n" +
                $"总对照表:\n{CocosImportSettings.UnityImportRoot}/POPUP_IMPORT_MASTER.md",
                "确定");
        }

        [MenuItem("AsGame/Cocos Import/Rebuild UUID Index")]
        public static void RebuildIndex()
        {
            var assets = Path.Combine(CocosImportSettings.CocosProjectRoot, "assets");
            CocosUuidIndex.Rebuild(assets);
        }

        [MenuItem("AsGame/Cocos Import/Import Cocos Prefab File...")]
        public static void ImportFromFileDialog()
        {
            var cocosAssets = Path.Combine(CocosImportSettings.CocosProjectRoot, "assets");
            var start = Directory.Exists(cocosAssets) ? cocosAssets : CocosImportSettings.CocosProjectRoot;
            var path = EditorUtility.OpenFilePanel("选择 Cocos Prefab", start, "prefab");
            if (string.IsNullOrEmpty(path)) return;
            Import(path, showDialog: true);
        }

        [MenuItem("AsGame/Cocos Import/仅重新生成总对照表")]
        public static void RegenerateMasterReportOnly()
        {
            var results = ScanAllWithoutImport();
            WriteMasterReport(results);
            EditorUtility.DisplayDialog("完成", "已更新 POPUP_IMPORT_MASTER.md（未重新导入 Prefab）", "确定");
        }

        public static List<CocosImportResult> ImportAllPopups(bool showDialogs)
        {
            var assets = Path.Combine(CocosImportSettings.CocosProjectRoot, "assets");
            if (!Directory.Exists(assets))
            {
                EditorUtility.DisplayDialog("错误", "Cocos assets 目录不存在，请检查 Settings 路径。", "确定");
                return new List<CocosImportResult>();
            }

            CocosUuidIndex.Rebuild(assets);
            var results = new List<CocosImportResult>();
            var entries = CocosPopupBatchCatalog.All;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                EditorUtility.DisplayProgressBar("导入 Cocos 弹窗", e.PrefabName,
                    (float)i / entries.Count);
                var cocosPath = Path.Combine(assets, e.RelativeCocosPath.Replace('/', Path.DirectorySeparatorChar));
                results.Add(ImportEntry(e, cocosPath));
            }

            EditorUtility.ClearProgressBar();
            return results;
        }

        static List<CocosImportResult> ScanAllWithoutImport()
        {
            var assets = Path.Combine(CocosImportSettings.CocosProjectRoot, "assets");
            var results = new List<CocosImportResult>();
            foreach (var e in CocosPopupBatchCatalog.All)
            {
                var cocosPath = Path.Combine(assets, e.RelativeCocosPath.Replace('/', Path.DirectorySeparatorChar));
                var r = new CocosImportResult
                {
                    PrefabName = e.PrefabName,
                    Category = e.Category,
                    CocosSource = cocosPath,
                    UnityScript = e.UnityScript ?? "-"
                };
                if (!File.Exists(cocosPath))
                {
                    r.Success = false;
                    r.Error = "源文件不存在";
                }
                else
                {
                    try
                    {
                        var doc = CocosPrefabParser.Parse(cocosPath, null, e.SkipGm);
                        r.LabelCount = doc.LabelPaths.Count;
                        r.SpriteCount = doc.AllNodes.Count(n => !string.IsNullOrEmpty(n.SpriteUuid));
                        r.UnityPrefabPath = GetUnityOutputPath(e.PrefabName);
                        r.Success = File.Exists(r.UnityPrefabPath);
                    }
                    catch (Exception ex)
                    {
                        r.Success = false;
                        r.Error = ex.Message;
                    }
                }

                results.Add(r);
            }

            return results;
        }

        static CocosImportResult ImportEntry(CocosPopupBatchCatalog.Entry entry, string cocosPath)
        {
            var result = new CocosImportResult
            {
                PrefabName = entry.PrefabName,
                Category = entry.Category,
                CocosSource = cocosPath,
                UnityScript = entry.UnityScript ?? "-"
            };

            if (!File.Exists(cocosPath))
            {
                result.Success = false;
                result.Error = "源文件不存在";
                Debug.LogWarning($"[CocosImport] 跳过 {entry.PrefabName}: 文件不存在");
                return result;
            }

            try
            {
                var doc = CocosPrefabParser.Parse(cocosPath, null, entry.SkipGm);
                var rootGo = BuildNodeTree(doc.Root, doc.PrefabName);

                if (ScriptByPrefabName.TryGetValue(doc.PrefabName, out var scriptType))
                {
                    if (rootGo.GetComponent(scriptType) == null)
                        rootGo.AddComponent(scriptType);
                    result.UnityScript = scriptType.Name;
                }

                var outPath = GetUnityOutputPath(doc.PrefabName);
                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                PrefabUtility.SaveAsPrefabAsset(rootGo, outPath, out var ok);
                UnityEngine.Object.DestroyImmediate(rootGo);

                result.Success = ok;
                result.UnityPrefabPath = outPath;
                result.LabelCount = doc.LabelPaths.Count;
                result.SpriteCount = doc.AllNodes.Count(n => !string.IsNullOrEmpty(n.SpriteUuid));

                if (ok)
                {
                    WriteLabelReport(doc, outPath);
                    Debug.Log($"[CocosImport] OK {doc.PrefabName} labels={result.LabelCount}");
                }
                else
                    result.Error = "SaveAsPrefabAsset 失败";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                Debug.LogError($"[CocosImport] 失败 {entry.PrefabName}: {ex}");
            }

            return result;
        }

        static string GetUnityOutputPath(string prefabName) =>
            Path.Combine(CocosImportSettings.UnityImportRoot, "UI", prefabName + ".prefab").Replace('\\', '/');

        public static void Import(string cocosPrefabPath, bool showDialog = true)
        {
            var name = Path.GetFileNameWithoutExtension(cocosPrefabPath);
            var entry = CocosPopupBatchCatalog.All.FirstOrDefault(e => e.PrefabName == name);
            CocosImportResult result;
            if (entry.PrefabName != null)
            {
                CocosUuidIndex.Rebuild(Path.Combine(CocosImportSettings.CocosProjectRoot, "assets"));
                result = ImportEntry(entry, cocosPrefabPath);
            }
            else
            {
                result = ImportEntry(
                    new CocosPopupBatchCatalog.Entry(name, cocosPrefabPath, "自定义"), cocosPrefabPath);
            }

            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Cocos Import",
                    result.Success
                        ? $"成功\n{result.UnityPrefabPath}\nLabel: {result.LabelCount}"
                        : $"失败\n{result.Error}",
                    "确定");
                if (result.Success)
                    EditorGUIUtility.PingObject(
                        AssetDatabase.LoadAssetAtPath<GameObject>(result.UnityPrefabPath));
            }
        }

        public static void WriteMasterReport(List<CocosImportResult> results)
        {
            var root = CocosImportSettings.UnityImportRoot.Replace('\\', '/');
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            var mdPath = Path.Combine(root, "POPUP_IMPORT_MASTER.md").Replace('\\', '/');
            var csvPath = Path.Combine(root, "POPUP_IMPORT_MASTER.csv").Replace('\\', '/');
            var sb = new StringBuilder();

            sb.AppendLine("# 弹窗 Prefab 导入总对照表");
            sb.AppendLine();
            sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cocos 工程: `{CocosImportSettings.CocosProjectRoot}`");
            sb.AppendLine($"Unity 输出: `{root}`");
            sb.AppendLine();
            sb.AppendLine("## 一键导入");
            sb.AppendLine("Unity 菜单: **AsGame → Cocos Import → ★ 一键导入全部弹窗 Prefab**");
            sb.AppendLine();
            sb.AppendLine("## 主表");
            sb.AppendLine();
            sb.AppendLine("| 分类 | Cocos 源文件 | Unity Prefab | UI 脚本 | Label 数 | Sprite 节点 | 状态 | 备注 |");
            sb.AppendLine("|------|-------------|--------------|---------|----------|-------------|------|------|");

            var csv = new StringBuilder();
            csv.AppendLine("分类,Cocos源,Unity Prefab,UI脚本,Label数,Sprite节点,状态,备注");

            foreach (var r in results)
            {
                var status = r.Success ? "✅" : "❌";
                var note = r.Error ?? "";
                var cocosRel = TryMakeRelative(r.CocosSource);
                sb.AppendLine(
                    $"| {r.Category} | `{cocosRel}` | `{r.UnityPrefabPath}` | {r.UnityScript} | {r.LabelCount} | {r.SpriteCount} | {status} | {note} |");
                csv.AppendLine(
                    $"{r.Category},{EscapeCsv(cocosRel)},{EscapeCsv(r.UnityPrefabPath)},{r.UnityScript},{r.LabelCount},{r.SpriteCount},{status},{EscapeCsv(note)}");
            }

            sb.AppendLine();
            sb.AppendLine("## 已排除（Google Play 不导入）");
            sb.AppendLine();
            foreach (var ex in CocosPopupBatchCatalog.ExcludedGooglePlay)
                sb.AppendLine($"- **{ex.PrefabName}** — {ex.Category}: `{ex.RelativeCocosPath}`");

            sb.AppendLine();
            sb.AppendLine("## PopupType 与 Unity 路径对应");
            sb.AppendLine();
            sb.AppendLine("| PopupType | 建议 Prefab 路径 |");
            sb.AppendLine("|-----------|------------------|");
            AppendPopupTypeMap(sb);

            sb.AppendLine();
            sb.AppendLine("## 各弹窗 Label 明细");
            sb.AppendLine();
            foreach (var r in results.Where(x => x.Success))
            {
                var labelFile = r.UnityPrefabPath.Replace(".prefab", ".labels.txt");
                if (!File.Exists(labelFile)) continue;
                sb.AppendLine($"### {r.PrefabName}");
                sb.AppendLine();
                sb.AppendLine($"源: `{TryMakeRelative(r.CocosSource)}`");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(File.ReadAllText(labelFile).TrimEnd());
                sb.AppendLine("```");
                sb.AppendLine();
            }

            File.WriteAllText(mdPath, sb.ToString(), Encoding.UTF8);
            File.WriteAllText(csvPath, csv.ToString(), Encoding.UTF8);
            Debug.Log($"[CocosImport] 总对照表: {mdPath}");
        }

        static void AppendPopupTypeMap(StringBuilder sb)
        {
            var map = new (string type, string prefab)[]
            {
                ("Setting", "SettingPopup"),
                ("Success", "SuccessPopup"),
                ("Rank", "RankPopup"),
                ("Collect", "CollectPopup"),
                ("GetCollect", "GetCollectPopup"),
                ("RecoverHeart", "RecoverHeartPopup"),
                ("GetHeart", "GetHeartPopup"),
                ("DailyHeart", "DailyHeartPopup"),
                ("NewPlay", "NewPlayPopup"),
                ("Feedback", "FeedbackPopup"),
            };
            var root = CocosImportSettings.UnityImportRoot;
            foreach (var (type, prefab) in map)
                sb.AppendLine($"| {type} | `{root}/UI/{prefab}.prefab` |");
        }

        static string TryMakeRelative(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return "";
            var root = CocosImportSettings.CocosProjectRoot.Replace('\\', '/');
            var abs = absolute.Replace('\\', '/');
            return abs.StartsWith(root) ? abs.Substring(root.Length).TrimStart('/') : abs;
        }

        static string EscapeCsv(string s) =>
            string.IsNullOrEmpty(s) ? "" : $"\"{s.Replace("\"", "\"\"")}\"";

        static void WriteLabelReport(CocosPrefabDocument doc, string prefabPath)
        {
            var reportPath = prefabPath.Replace(".prefab", ".labels.txt");
            var lines = new List<string>
            {
                "# 从 Cocos 自动提取的 Label 文本",
                "# 源文件: " + doc.SourcePath,
                ""
            };
            foreach (var (path, text) in doc.LabelPaths)
                lines.Add($"{path}\t{text}");
            File.WriteAllLines(reportPath, lines, Encoding.UTF8);
        }

        static GameObject BuildNodeTree(CocosNodeData node, string rootName)
        {
            var go = new GameObject(node.Name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = node.Size;
            rt.anchoredPosition = node.Position;
            go.SetActive(node.Active);

            if (!string.IsNullOrEmpty(node.SpriteUuid))
            {
                var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                var sp = LoadSprite(node.SpriteUuid);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.SetNativeSize();
                }

                img.color = new Color(node.Color.r, node.Color.g, node.Color.b, node.Opacity / 255f);
            }

            if (!string.IsNullOrEmpty(node.LabelText))
            {
                var text = go.GetComponent<Text>() ?? go.AddComponent<Text>();
                text.text = node.LabelText;
                text.fontSize = node.FontSize;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = node.Color;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                StretchFull(text.GetComponent<RectTransform>());
            }

            if (node.HasButton)
            {
                var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                var img = go.GetComponent<Image>();
                if (img != null) btn.targetGraphic = img;
            }

            foreach (var child in node.ChildNodes)
            {
                var childGo = BuildNodeTree(child, rootName);
                childGo.transform.SetParent(go.transform, false);
            }

            return go;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static Sprite LoadSprite(string cocosSpriteUuid)
        {
            if (!CocosUuidIndex.TryResolve(cocosSpriteUuid, out var absPath))
                return null;

            var cocosRoot = Path.Combine(CocosImportSettings.CocosProjectRoot, "assets").Replace('\\', '/');
            var relative = absPath.Replace('\\', '/');
            if (relative.StartsWith(cocosRoot))
                relative = relative.Substring(cocosRoot.Length).TrimStart('/');

            var unityPath = Path.Combine(CocosImportSettings.UnityImportRoot, "Textures", relative)
                .Replace('\\', '/');
            var dir = Path.GetDirectoryName(unityPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(unityPath))
                File.Copy(absPath, unityPath, true);

            AssetDatabase.ImportAsset(unityPath, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.Sprite)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(unityPath);
        }
    }
}
#endif
