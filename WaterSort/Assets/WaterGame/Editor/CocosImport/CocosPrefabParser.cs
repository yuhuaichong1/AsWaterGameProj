#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AsGame.Editor.CocosImport
{
    public sealed class CocosNodeData
    {
        public int Id;
        public string Name;
        public int? ParentId;
        public List<int> Children = new();
        public Vector2 Position;
        public Vector2 Size;
        public float Opacity = 255f;
        public bool Active = true;
        public Color Color = Color.white;
        public string LabelText;
        public int FontSize = 24;
        public string SpriteUuid;
        public bool HasButton;
        public List<CocosNodeData> ChildNodes = new();
    }

    public sealed class CocosPrefabDocument
    {
        public string PrefabName;
        public string SourcePath;
        public CocosNodeData Root;
        public List<CocosNodeData> AllNodes = new();
        public List<(string path, string text)> LabelPaths = new();
    }

    public static class CocosPrefabParser
    {
        public static CocosPrefabDocument Parse(string prefabJsonPath, string contentRootName = "content",
            bool skipGm = true)
        {
            var json = File.ReadAllText(prefabJsonPath);
            var arr = JArray.Parse(json);
            var map = new Dictionary<int, JObject>();
            for (var i = 0; i < arr.Count; i++)
                map[i] = (JObject)arr[i];

            var prefabName = Path.GetFileNameWithoutExtension(prefabJsonPath);
            var rootId = 1;
            if (map[0]["data"]?["__id__"] != null)
                rootId = map[0]["data"]["__id__"].Value<int>();

            var nodes = new Dictionary<int, CocosNodeData>();
            foreach (var kv in map)
            {
                if (kv.Value["__type__"]?.Value<string>() != "cc.Node") continue;
                var n = ParseNode(kv.Key, kv.Value);
                nodes[n.Id] = n;
            }

            foreach (var n in nodes.Values)
            {
                if (n.ParentId.HasValue && nodes.TryGetValue(n.ParentId.Value, out var parent))
                    parent.ChildNodes.Add(n);
            }

            AttachComponents(map, nodes);

            var root = nodes[rootId];
            var doc = new CocosPrefabDocument
            {
                PrefabName = prefabName,
                SourcePath = prefabJsonPath,
                Root = root
            };
            Flatten(root, "", doc);

            if (!string.IsNullOrEmpty(contentRootName))
            {
                var content = FindByName(root, contentRootName);
                if (content != null)
                    doc.Root = content;
            }

            if (skipGm)
                PruneByName(doc.Root, "gm");

            return doc;
        }

        static void PruneByName(CocosNodeData node, string name)
        {
            node.ChildNodes.RemoveAll(c => c.Name == name);
            foreach (var c in node.ChildNodes)
                PruneByName(c, name);
        }

        static CocosNodeData FindByName(CocosNodeData node, string name)
        {
            if (node.Name == name) return node;
            foreach (var c in node.ChildNodes)
            {
                var f = FindByName(c, name);
                if (f != null) return f;
            }

            return null;
        }

        static void Flatten(CocosNodeData node, string path, CocosPrefabDocument doc)
        {
            var p = string.IsNullOrEmpty(path) ? node.Name : path + "/" + node.Name;
            doc.AllNodes.Add(node);
            if (!string.IsNullOrEmpty(node.LabelText))
                doc.LabelPaths.Add((p, node.LabelText));
            foreach (var c in node.ChildNodes)
                Flatten(c, p, doc);
        }

        static CocosNodeData ParseNode(int id, JObject o)
        {
            var n = new CocosNodeData { Id = id, Name = o["_name"]?.Value<string>() ?? "node" };
            if (o["_parent"]?["__id__"] != null)
                n.ParentId = o["_parent"]["__id__"].Value<int>();
            if (o["_children"] is JArray ch)
                foreach (var c in ch)
                    n.Children.Add(c["__id__"]?.Value<int>() ?? 0);

            if (o["_contentSize"] != null)
            {
                n.Size = new Vector2(
                    o["_contentSize"]["width"]?.Value<float>() ?? 0,
                    o["_contentSize"]["height"]?.Value<float>() ?? 0);
            }

            if (o["_trs"]?["array"] is JArray trs && trs.Count >= 2)
            {
                n.Position = new Vector2(trs[0].Value<float>(), trs[1].Value<float>());
            }

            if (o["_opacity"] != null)
                n.Opacity = o["_opacity"].Value<float>();
            if (o["_active"] != null)
                n.Active = o["_active"].Value<bool>();
            if (o["_color"] != null)
            {
                var c = o["_color"];
                n.Color = new Color(
                    (c["r"]?.Value<int>() ?? 255) / 255f,
                    (c["g"]?.Value<int>() ?? 255) / 255f,
                    (c["b"]?.Value<int>() ?? 255) / 255f,
                    (c["a"]?.Value<int>() ?? 255) / 255f);
            }

            return n;
        }

        static void AttachComponents(Dictionary<int, JObject> map, Dictionary<int, CocosNodeData> nodes)
        {
            foreach (var kv in map)
            {
                var type = kv.Value["__type__"]?.Value<string>();
                var nodeId = kv.Value["node"]?["__id__"]?.Value<int>();
                if (!nodeId.HasValue || !nodes.TryGetValue(nodeId.Value, out var node)) continue;

                switch (type)
                {
                    case "cc.Label":
                        node.LabelText = kv.Value["_string"]?.Value<string>()
                                         ?? kv.Value["_N$string"]?.Value<string>();
                        node.FontSize = kv.Value["_fontSize"]?.Value<int>() ?? 24;
                        break;
                    case "cc.Sprite":
                        node.SpriteUuid = kv.Value["_spriteFrame"]?["__uuid__"]?.Value<string>();
                        break;
                    case "cc.Button":
                        node.HasButton = true;
                        break;
                }
            }
        }
    }
}
#endif
