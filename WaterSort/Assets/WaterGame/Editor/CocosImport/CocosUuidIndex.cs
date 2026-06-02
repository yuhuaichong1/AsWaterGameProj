#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AsGame.Editor.CocosImport
{
    /// <summary>扫描 Cocos assets 下 .meta，建立 sprite-frame uuid → png 相对路径索引。</summary>
    public static class CocosUuidIndex
    {
        static Dictionary<string, string> _uuidToAssetPath;
        static string _root;

        public static void Rebuild(string cocosAssetsRoot)
        {
            _root = cocosAssetsRoot.Replace('\\', '/');
            _uuidToAssetPath = new Dictionary<string, string>();
            if (!Directory.Exists(_root))
            {
                Debug.LogError("[CocosUuidIndex] 目录不存在: " + _root);
                return;
            }

            var metas = Directory.GetFiles(_root, "*.meta", SearchOption.AllDirectories);
            var uuidInMeta = new Regex("\"uuid\"\\s*:\\s*\"([a-f0-9\\-]+)\"", RegexOptions.IgnoreCase);
            foreach (var metaPath in metas)
            {
                var text = File.ReadAllText(metaPath);
                var assetPath = metaPath.Substring(0, metaPath.Length - 5).Replace('\\', '/');
                if (!assetPath.EndsWith(".png") && !assetPath.EndsWith(".jpg")) continue;

                foreach (Match m in uuidInMeta.Matches(text))
                {
                    var id = m.Groups[1].Value;
                    if (!_uuidToAssetPath.ContainsKey(id))
                        _uuidToAssetPath[id] = assetPath;
                }
            }

            Debug.Log($"[CocosUuidIndex] 索引 {_uuidToAssetPath.Count} 个贴图 UUID");
        }

        public static bool TryResolve(string uuid, out string absoluteAssetPath)
        {
            absoluteAssetPath = null;
            if (_uuidToAssetPath == null) return false;
            if (string.IsNullOrEmpty(uuid)) return false;
            if (_uuidToAssetPath.TryGetValue(uuid, out var rel))
            {
                absoluteAssetPath = rel;
                return File.Exists(rel);
            }

            return false;
        }
    }
}
#endif
