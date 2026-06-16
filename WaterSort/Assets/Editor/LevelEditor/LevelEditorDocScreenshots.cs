#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>
    /// 导出关卡编辑器文档用截图到 Doc/LevelEditor/images/。
    /// 菜单：水排序 → 关卡编辑器 → 导出文档截图
    /// </summary>
    public static class LevelEditorDocScreenshots
    {
        const string ImagesFolder = "Doc/LevelEditor/images";

        [MenuItem("水排序/关卡编辑器/导出文档截图")]
        public static void CaptureFromMenu()
        {
            var window = EditorWindow.GetWindow<LevelEditorWindow>("水排序关卡");
            window.minSize = new Vector2(1200, 720);
            window.Show();
            window.Repaint();
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () => CaptureAll(window);
            };
        }

        static void CaptureAll(LevelEditorWindow window)
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ImagesFolder));
            Directory.CreateDirectory(dir);

            if (!TryCaptureWindow(window, Path.Combine(dir, "01_level_editor_full.png"), out var msg))
            {
                EditorUtility.DisplayDialog("导出文档截图", "失败：" + msg, "确定");
                return;
            }

            EditorUtility.DisplayDialog(
                "导出文档截图",
                $"已保存到：\n{dir}\n\n包含：01_level_editor_full.png\n请重新打开 Doc/LevelEditor/关卡编辑器需求方案.html 查看。",
                "确定");
            EditorUtility.RevealInFinder(dir);
        }

        static bool TryCaptureWindow(EditorWindow window, string path, out string error)
        {
            error = null;
            if (window == null)
            {
                error = "找不到关卡编辑器窗口";
                return false;
            }

            window.Repaint();
            var rect = window.position;
            var width = Mathf.Max(1, (int)rect.width);
            var height = Mathf.Max(1, (int)rect.height);

            try
            {
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                return true;
            }
            catch (System.Exception ex)
            {
                // ReadPixels 在部分 Unity 版本需从 OnGUI 回调；回退全屏裁剪
                try
                {
                    var fullPath = Path.Combine(Application.temporaryCachePath, "level_editor_cap.png");
                    ScreenCapture.CaptureScreenshot(fullPath);
                    AssetDatabase.Refresh();
                    if (!File.Exists(fullPath))
                    {
                        error = ex.Message;
                        return false;
                    }

                    var bytes = File.ReadAllBytes(fullPath);
                    var full = new Texture2D(2, 2);
                    full.LoadImage(bytes);
                    var x = Mathf.Clamp((int)rect.x, 0, full.width - 1);
                    var y = Mathf.Clamp(full.height - (int)rect.y - height, 0, full.height - height);
                    var cropped = new Texture2D(width, height, TextureFormat.RGB24, false);
                    var pixels = full.GetPixels(x, y, Mathf.Min(width, full.width - x), Mathf.Min(height, full.height - y));
                    cropped.SetPixels(pixels);
                    cropped.Apply();
                    File.WriteAllBytes(path, cropped.EncodeToPNG());
                    Object.DestroyImmediate(full);
                    Object.DestroyImmediate(cropped);
                    return true;
                }
                catch (System.Exception ex2)
                {
                    error = ex.Message + " / " + ex2.Message;
                    return false;
                }
            }
        }
    }
}
#endif
