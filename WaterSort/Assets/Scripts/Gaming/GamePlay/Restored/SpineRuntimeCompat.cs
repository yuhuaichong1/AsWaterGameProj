using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AsGame.Spine
{
    /// <summary>检测 Spine JSON 版本；3.x 资源不应在 4.2 运行时上加载。</summary>
    public static class SpineRuntimeCompat
    {
        static readonly Regex VersionRegex = new(@"""spine""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
        static readonly Dictionary<string, bool> Legacy38ByFolder = new();

        public static bool IsSpine38Json(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText)) return false;
            var match = VersionRegex.Match(jsonText);
            if (!match.Success) return false;
            return match.Groups[1].Value.StartsWith("3.");
        }

        public static bool IsLegacy38Folder(string folder)
        {
            if (Legacy38ByFolder.TryGetValue(folder, out var cached))
                return cached;

            var json = LoadFolderJson(folder);
            var legacy = json != null && IsSpine38Json(json.text);
            Legacy38ByFolder[folder] = legacy;
            return legacy;
        }

        public static bool CanUseSpineUnity(string folder) => !IsLegacy38Folder(folder);

        public static void ClearCache() => Legacy38ByFolder.Clear();

        static TextAsset LoadFolderJson(string folder)
        {
            var basePath = "Spine/" + folder;
            var jsonName = folder switch
            {
                "shui_hua" => "sh",
                "wht" => "js",
                _ => folder
            };

            foreach (var asset in Resources.LoadAll<TextAsset>(basePath))
            {
                if (asset != null && asset.name == jsonName)
                    return asset;
            }

            return Resources.Load<TextAsset>($"{basePath}/{jsonName}");
        }
    }
}
