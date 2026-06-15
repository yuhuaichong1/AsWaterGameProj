using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using XrCode;

namespace AsGame.Spine
{
    /// <summary>解析 Spine atlas + json，用图集帧播放简易动画（无需 spine-unity）。</summary>
    public static class SpineAtlasPlayer
    {
        static readonly Dictionary<string, AtlasCache> Cache = new();

        class AtlasCache
        {
            public Texture2D Texture;
            public Dictionary<string, AtlasRegion> Regions = new();
            public Dictionary<string, List<FrameKey>> Animations = new();
        }

        public class AtlasRegion
        {
            public Sprite Sprite;
            public float RotationZ;
        }

        public class AtlasCachePublic
        {
            public Dictionary<string, AtlasRegion> Regions = new();
        }

        public static AtlasCachePublic LoadCache(string folder)
        {
            var cache = GetOrLoad(folder);
            if (cache == null) return null;
            var pub = new AtlasCachePublic();
            foreach (var pair in cache.Regions)
                pub.Regions[pair.Key] = pair.Value;
            return pub;
        }

        public static TextAsset FindTextAssetPublic(string basePath, params string[] names) =>
            FindTextAsset(basePath, names);

        public static string ExtractAnimBlockPublic(string json, string animName) =>
            ExtractAnimBlock(json, animName);

        public static void ApplyRegionPublic(Image img, RectTransform rt, AtlasRegion region) =>
            ApplyRegion(img, rt, region);

        struct FrameKey
        {
            public float Time;
            public string Region;
        }

        public static bool TryPlay(Transform host, string folder, string animationName, bool loop, Action onComplete,
            Color? tint = null, float? duration = null)
        {
            var cache = GetOrLoad(folder);
            if (cache == null) return false;

            var resolvedAnim = ResolveAnimationName(folder, animationName);
            if (!cache.Animations.TryGetValue(resolvedAnim, out var frames) || frames.Count == 0)
            {
                if (!cache.Animations.TryGetValue(GetDefaultAnim(folder), out frames) || frames.Count == 0)
                    return ShouldUseBonePulseFallback(folder)
                        && TryPlayBonePulse(host, cache, loop, onComplete, tint, duration);
            }

            if (frames == null || frames.Count == 0)
                return ShouldUseBonePulseFallback(folder)
                    && TryPlayBonePulse(host, cache, loop, onComplete, tint, duration);

            SpineRunner.Instance.StartCoroutine(PlayFrames(host, cache, frames, loop, onComplete, tint, duration));
            return true;
        }

        static string ResolveAnimationName(string folder, string animationName)
        {
            if (folder == "xuan_zhong" && animationName == "guang")
                return "idle";
            return animationName;
        }

        static AtlasCache GetOrLoad(string folder)
        {
            if (Cache.TryGetValue(folder, out var cached))
                return cached;

            var basePath = "Spine/" + folder;
            var fileName = GetAtlasFileName(folder);
            var atlasText = FindTextAsset(basePath, fileName + ".atlas", fileName + ".atlas.txt");
            var tex = LoadTexture(basePath, fileName);
            if (atlasText == null || tex == null) return null;

            var cache = new AtlasCache { Texture = tex };
            ParseAtlas(atlasText.text, tex, cache.Regions);
            var jsonText = FindTextAsset(basePath, GetJsonFileName(folder));
            if (jsonText != null)
            {
                foreach (var animName in GetAnimNamesToParse(folder))
                    ParseAnimation(jsonText.text, animName, cache.Animations);
            }

            Cache[folder] = cache;
            return cache;
        }

        static IEnumerable<string> GetAnimNamesToParse(string folder)
        {
            yield return GetDefaultAnim(folder);
            if (folder == "xuan_zhong")
            {
                yield return "idle";
                yield return "guang";
            }
            if (folder == "shui")
                yield return "huang";
        }

        static TextAsset FindTextAsset(string basePath, params string[] names)
        {
            foreach (var asset in Resources.LoadAll<TextAsset>(basePath))
            {
                if (asset == null) continue;
                foreach (var name in names)
                {
                    if (asset.name == name)
                        return asset;
                }
            }

            foreach (var name in names)
            {
                var asset = Resources.Load<TextAsset>($"{basePath}/{name}");
                if (asset != null)
                    return asset;
            }

            return null;
        }

        static Texture2D LoadTexture(string basePath, string fileName)
        {
            var tex = Resources.Load<Texture2D>($"{basePath}/{fileName}");
            if (tex != null)
                return tex;

            var sprite = GameResourceLoader.LoadSprite($"{basePath}/{fileName}");
            return sprite != null ? sprite.texture : null;
        }

        static bool TryPlayBonePulse(Transform host, AtlasCache cache, bool loop, Action onComplete, Color? tint = null,
            float? duration = null)
        {
            AtlasRegion region = null;
            foreach (var entry in cache.Regions.Values)
            {
                region = entry;
                break;
            }

            if (region?.Sprite == null) return false;
            SpineRunner.Instance.StartCoroutine(PlayBonePulse(host, region, loop, onComplete, tint, duration));
            return true;
        }

        static IEnumerator PlayBonePulse(Transform host, AtlasRegion region, bool loop, Action onComplete, Color? tint = null,
            float? duration = null)
        {
            var go = new GameObject("SpineFx_AtlasPulse", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            ApplyRegion(img, rt, region);
            img.raycastTarget = false;
            img.color = tint ?? Color.white;
            // 打乱光环略放大，贴近 Cocos Spine_Shuffle 视觉
            rt.localScale = Vector3.one * 1.35f;
            var cg = go.GetComponent<CanvasGroup>();

            if (duration.HasValue)
            {
                var elapsed = 0f;
                while (elapsed < duration.Value && host != null && go != null)
                {
                    yield return TweenHelper.ToFloatWhile(go, 0.85f, 1.15f, 0.175f, s =>
                    {
                        rt.localScale = Vector3.one * s;
                        cg.alpha = Mathf.Lerp(0.55f, 1f, (s - 0.85f) / 0.3f);
                    });
                    elapsed += 0.175f;
                    if (elapsed >= duration.Value || go == null) break;
                    yield return TweenHelper.ToFloatWhile(go, 1.15f, 0.85f, 0.175f, s =>
                    {
                        rt.localScale = Vector3.one * s;
                        cg.alpha = Mathf.Lerp(1f, 0.55f, (1.15f - s) / 0.3f);
                    });
                    elapsed += 0.175f;
                }

                if (go != null)
                {
                    var startAlpha = cg.alpha;
                    var startScale = rt.localScale.x;
                    var startColor = img.color;
                    yield return TweenHelper.ToFloat(0f, 1f, 0.35f, t =>
                    {
                        if (go == null) return;
                        cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
                        rt.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * 0.94f, t);
                        var c = startColor;
                        c.a = Mathf.Lerp(startColor.a, 0f, t);
                        img.color = c;
                    });
                }

                if (go != null) UnityEngine.Object.Destroy(go);
                onComplete?.Invoke();
                yield break;
            }

            do
            {
                yield return TweenHelper.ToFloatWhile(go, 0.85f, 1.15f, 0.35f, s =>
                {
                    rt.localScale = Vector3.one * s;
                    cg.alpha = Mathf.Lerp(0.55f, 1f, (s - 0.85f) / 0.3f);
                });
                if (!loop) break;
                yield return TweenHelper.ToFloatWhile(go, 1.15f, 0.85f, 0.35f, s =>
                {
                    rt.localScale = Vector3.one * s;
                    cg.alpha = Mathf.Lerp(1f, 0.55f, (1.15f - s) / 0.3f);
                });
            } while (loop && host != null && go != null);

            if (go != null) UnityEngine.Object.Destroy(go);
            onComplete?.Invoke();
        }

        static string GetAtlasFileName(string folder) =>
            folder switch
            {
                "shui_hua" => "sh",
                "bao_xing" => "bao_xing",
                "xuan_zhong" => "xuan_zhong",
                "he_cheng_1" => "he_cheng_1",
                "shui" => "shui",
                "wht" => "js",
                "dai_zi" => "dai_zi",
                _ => folder
            };

        static string GetJsonFileName(string folder) => GetAtlasFileName(folder);

        static bool ShouldUseBonePulseFallback(string folder) =>
            folder is not ("bao_xing" or "dai_zi" or "he_cheng_2");

        static string GetDefaultAnim(string folder) =>
            folder switch
            {
                "shui_hua" => "shuihua",
                "bao_xing" => "bao",
                "xuan_zhong" => "idle",
                "he_cheng_1" => "zhuang",
                "shui" => "huang",
                "wht" => "animation2",
                "dai_zi" => "zhuang",
                _ => "animation"
            };

        static void ParseAtlas(string atlas, Texture2D tex, Dictionary<string, AtlasRegion> regions)
        {
            var lines = atlas.Split('\n');
            string current = null;
            int x = 0, y = 0, w = 0, h = 0;
            var rotate = false;
            var texH = tex.height;
            var texW = tex.width;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("format:") ||
                    line.StartsWith("filter:") || line.StartsWith("repeat:") || line.EndsWith(".png"))
                    continue;

                if (!line.Contains(":"))
                {
                    current = line;
                    rotate = false;
                    continue;
                }

                if (current == null) continue;

                if (line.StartsWith("rotate:"))
                {
                    rotate = line.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                else if (line.StartsWith("xy:"))
                {
                    var p = line.Substring(3).Split(',');
                    x = int.Parse(p[0].Trim());
                    y = int.Parse(p[1].Trim());
                }
                else if (line.StartsWith("size:"))
                {
                    var p = line.Substring(5).Split(',');
                    w = int.Parse(p[0].Trim());
                    h = int.Parse(p[1].Trim());

                    // Spine rotate:true 表示图块在图集中顺时针旋转 90°，占用宽高与 size 字段互换。
                    var packedW = rotate ? h : w;
                    var packedH = rotate ? w : h;
                    var rect = new Rect(x, texH - y - packedH, packedW, packedH);
                    var sprite = TryCreateSprite(tex, rect, texW, texH);
                    if (sprite != null)
                    {
                        regions[current] = new AtlasRegion
                        {
                            Sprite = sprite,
                            RotationZ = rotate ? -90f : 0f
                        };
                    }

                    current = null;
                    rotate = false;
                }
            }
        }

        static Sprite TryCreateSprite(Texture2D tex, Rect rect, int texW, int texH)
        {
            if (rect.xMax > texW || rect.yMax > texH || rect.width <= 0 || rect.height <= 0)
                return null;

            try
            {
                return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        static void ApplyRegion(Image img, RectTransform rt, AtlasRegion region)
        {
            img.sprite = region.Sprite;
            img.SetNativeSize();
            rt.localRotation = Quaternion.Euler(0f, 0f, region.RotationZ);
        }

        static void ParseAnimation(string json, string animationName, Dictionary<string, List<FrameKey>> anims)
        {
            var animBlock = ExtractAnimBlock(json, animationName);
            if (animBlock == null) return;

            var slotPattern = new Regex("\"attachment\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            var matches = slotPattern.Matches(animBlock);
            var frames = new List<FrameKey>();

            foreach (Match slotMatch in matches)
            {
                var keyPattern = new Regex("\\{\"time\"\\s*:\\s*([0-9.]+)?(?:,\"name\"\\s*:\\s*\"([^\"]+)\")?|\\{\"name\"\\s*:\\s*\"([^\"]+)\"\\}");
                foreach (Match m in keyPattern.Matches(slotMatch.Groups[1].Value))
                {
                    var time = m.Groups[1].Success ? float.Parse(m.Groups[1].Value) : 0f;
                    var name = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                    if (name == "null") continue;
                    frames.Add(new FrameKey { Time = time, Region = name });
                }
            }

            frames.Sort((a, b) => a.Time.CompareTo(b.Time));
            if (frames.Count > 0)
                anims[animationName] = frames;
        }

        static string ExtractAnimBlock(string json, string animName)
        {
            var key = "\"animations\":{";
            var idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return null;
            var search = "\"" + animName + "\":{";
            var start = json.IndexOf(search, idx, StringComparison.Ordinal);
            if (start < 0) return null;
            start += search.Length;
            var depth = 1;
            for (var i = start; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return json.Substring(start, i - start);
                }
            }

            return null;
        }

        static IEnumerator PlayFrames(Transform host, AtlasCache cache, List<FrameKey> frames, bool loop, Action onComplete,
            Color? tint = null, float? playDuration = null)
        {
            var go = new GameObject("SpineFx_Atlas", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            if (tint.HasValue) img.color = tint.Value;
            var cg = go.GetComponent<CanvasGroup>();

            var animDuration = frames[^1].Time + 0.15f;
            if (animDuration < 0.25f) animDuration = 0.45f;
            var shouldLoop = playDuration.HasValue || loop;
            do
            {
                var elapsed = 0f;
                var frameIdx = 0;
                var cycleLimit = playDuration.HasValue ? playDuration.Value : animDuration;

                while (elapsed < cycleLimit && host != null && go != null)
                {
                    while (frameIdx + 1 < frames.Count && frames[frameIdx + 1].Time <= elapsed)
                        frameIdx++;

                    var region = frames[frameIdx].Region;
                    if (cache.Regions.TryGetValue(region, out var atlasRegion))
                        ApplyRegion(img, rt, atlasRegion);

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            } while (shouldLoop && !playDuration.HasValue && host != null && go != null);

            if (playDuration.HasValue && go != null)
            {
                var startAlpha = cg.alpha;
                var startScale = rt.localScale.x;
                var startColor = img.color;
                yield return TweenHelper.ToFloat(0f, 1f, 0.35f, t =>
                {
                    if (go == null) return;
                    cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
                    rt.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * 0.94f, t);
                    var c = startColor;
                    c.a = Mathf.Lerp(startColor.a, 0f, t);
                    img.color = c;
                });
            }

            if (go != null) UnityEngine.Object.Destroy(go);
            onComplete?.Invoke();
        }
    }
}
