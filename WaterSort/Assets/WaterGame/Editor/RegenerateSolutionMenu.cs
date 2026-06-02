using System.IO;
using UnityEditor;
using UnityEngine;

namespace AsGame.EditorTools
{
    /// <summary>在 Unity 内一键重新生成 Visual Studio 解决方案。</summary>
    public static class RegenerateSolutionMenu
    {
        [MenuItem("Tools/AsGame/Regenerate C# Solution")]
        public static void Regenerate()
        {
#if UNITY_2020_1_OR_NEWER
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
            Debug.Log("已请求 IDE 包重新生成 .sln / .csproj。");
#else
            Debug.LogWarning("当前 Unity 版本请手动在 Preferences > External Tools 中生成项目文件。");
#endif
        }

        [InitializeOnLoadMethod]
        static void EnsureSolutionExists()
        {
            var sln = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "AsGame.sln");
            if (File.Exists(sln)) return;
            EditorApplication.delayCall += Regenerate;
        }
    }
}
