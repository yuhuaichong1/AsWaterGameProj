using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using AsGame.Core;

namespace AsGame.Spine
{
    /// <summary>用 spine-unity SkeletonGraphic 播放 Resources 内 Spine 动画（选中特效等）。</summary>
    public static class SpineGraphicFxPlayer
    {
        static readonly Dictionary<string, SkeletonDataAsset> SkeletonCache = new();

        static readonly HashSet<string> SupportedFolders = new()
        {
            "shui", "shui_hua", "xuan_zhong", "bao_xing", "he_cheng_1", "he_cheng_2", "sheng_cheng", "wht", "dai_zi"
        };

        const float TimedEffectFadeOutDuration = 0.35f;

        public static bool TryPlay(Transform host, string folder, string animationName, bool loop, Action onComplete = null,
            Color? tint = null, float? playDuration = null)
        {
            if (host == null || !SupportedFolders.Contains(folder)) return false;

            var skeletonData = GetSkeletonData(folder);
            if (skeletonData == null) return false;

            SpineRunner.Instance.StartCoroutine(PlayGraphic(host, skeletonData, folder, animationName, loop, onComplete, tint,
                playDuration));
            return true;
        }

        static IEnumerator PlayGraphic(Transform host, SkeletonDataAsset skeletonData, string folder, string animationName,
            bool loop, Action onComplete, Color? tint, float? playDuration)
        {
            if (host == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var go = new GameObject("SpineFx_Graphic", typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            var graphic = go.AddComponent<SkeletonGraphic>();
            graphic.skeletonDataAsset = skeletonData;
            graphic.material = GetGraphicMaterial(folder);
            graphic.raycastTarget = false;
            graphic.startingLoop = loop;
            graphic.Initialize(false);

            if (!graphic.IsValid)
            {
                UnityEngine.Object.Destroy(go);
                onComplete?.Invoke();
                yield break;
            }

            TrySetDefaultSkin(graphic, folder);
            if (tint.HasValue)
                graphic.color = tint.Value;
            graphic.MatchRectTransformWithBounds();

            var state = graphic.AnimationState;
            var resolvedAnim = ResolveAnimationName(folder, animationName);
            var shouldLoop = playDuration.HasValue || loop;
            var entry = state.SetAnimation(0, resolvedAnim, shouldLoop);
            if (entry == null)
            {
                var fallback = GetDefaultAnimation(folder);
                entry = state.SetAnimation(0, fallback, shouldLoop);
            }

            if (entry == null)
            {
                UnityEngine.Object.Destroy(go);
                onComplete?.Invoke();
                yield break;
            }

            if (playDuration.HasValue)
            {
                yield return new WaitForSeconds(playDuration.Value);
                if (go != null && graphic != null)
                    yield return FadeOutGraphic(graphic, rt, TimedEffectFadeOutDuration);
                if (go != null) UnityEngine.Object.Destroy(go);
                onComplete?.Invoke();
                yield break;
            }

            if (loop)
                yield break;

            var done = false;
            entry.Complete += _ =>
            {
                if (done) return;
                done = true;
                if (go != null) UnityEngine.Object.Destroy(go);
                onComplete?.Invoke();
            };

            var duration = entry.Animation != null ? entry.Animation.Duration : 0.45f;
            if (duration < 0.1f) duration = 0.45f;
            yield return new WaitForSeconds(duration + 0.05f);

            if (!done)
            {
                done = true;
                if (go != null) UnityEngine.Object.Destroy(go);
                onComplete?.Invoke();
            }
        }

        static IEnumerator FadeOutGraphic(SkeletonGraphic graphic, RectTransform rt, float fadeDuration)
        {
            if (graphic == null) yield break;

            var startColor = graphic.color;
            var startScale = rt != null ? rt.localScale.x : 1f;
            var state = graphic.AnimationState;
            var startTimeScale = state != null ? state.TimeScale : 1f;

            yield return TweenHelper.ToFloat(0f, 1f, fadeDuration, t =>
            {
                if (graphic == null) return;
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                graphic.color = c;
                if (state != null)
                    state.TimeScale = Mathf.Lerp(startTimeScale, 0.12f, t);
                if (rt != null)
                    rt.localScale = Vector3.one * Mathf.Lerp(startScale, startScale * 0.94f, t);
            });
        }

        static void TrySetDefaultSkin(SkeletonGraphic graphic, string folder)
        {
            if (graphic?.Skeleton == null) return;
            var skinName = folder switch
            {
                "shui" => "shang",
                _ => null
            };
            if (string.IsNullOrEmpty(skinName)) return;
            var skin = graphic.Skeleton.Data.FindSkin(skinName);
            if (skin == null) return;
            graphic.Skeleton.SetSkin(skin);
            graphic.Skeleton.SetSlotsToSetupPose();
        }

        static SkeletonDataAsset GetSkeletonData(string folder)
        {
            if (SkeletonCache.TryGetValue(folder, out var cached) && cached != null)
                return cached;

            var assetName = GetSkeletonAssetName(folder);
            var imported = Resources.Load<SkeletonDataAsset>($"Spine/{folder}/{assetName}_SkeletonData");
            if (imported != null)
            {
                try
                {
                    if (imported.GetSkeletonData(true) != null)
                    {
                        SkeletonCache[folder] = imported;
                        return imported;
                    }
                }
                catch
                {
                    imported.Clear();
                }
            }

            var runtime = CreateRuntimeSkeletonData(folder);
            if (runtime != null)
                SkeletonCache[folder] = runtime;
            return runtime;
        }

        static SkeletonDataAsset CreateRuntimeSkeletonData(string folder)
        {
            var jsonName = GetSkeletonAssetName(folder);
            var json = LoadTextAsset($"Spine/{folder}", jsonName);
            if (json == null || SpineRuntimeCompat.IsSpine38Json(json.text))
                return null;

            var importedAtlas = Resources.Load<SpineAtlasAsset>($"Spine/{folder}/{jsonName}_Atlas");
            if (importedAtlas != null)
                return SkeletonDataAsset.CreateRuntimeInstance(json, importedAtlas, true, 0.01f);

            var atlasText = LoadTextAsset($"Spine/{folder}", jsonName + ".atlas")
                            ?? LoadTextAsset($"Spine/{folder}", jsonName + ".atlas.txt");
            var tex = Resources.Load<Texture2D>($"Spine/{folder}/{jsonName}");
            if (tex == null)
            {
                var sp = GameResourceLoader.LoadSprite($"Spine/{folder}/{jsonName}");
                tex = sp != null ? sp.texture : null;
            }

            if (atlasText == null || tex == null)
                return null;

            tex.name = jsonName;
            var atlasAsset = SpineAtlasAsset.CreateRuntimeInstance(
                atlasText,
                new[] { tex },
                GetGraphicMaterial(folder),
                true);
            return SkeletonDataAsset.CreateRuntimeInstance(json, atlasAsset, true, 0.01f);
        }

        static TextAsset LoadTextAsset(string resourceFolder, string assetName)
        {
            var direct = Resources.Load<TextAsset>($"{resourceFolder}/{assetName}");
            if (direct != null) return direct;

            foreach (var asset in Resources.LoadAll<TextAsset>(resourceFolder))
            {
                if (asset != null && asset.name == assetName)
                    return asset;
            }

            return null;
        }

        static string ResolveAnimationName(string folder, string animationName) =>
            folder == "xuan_zhong" && animationName == "guang" ? "idle" : animationName;

        static string GetSkeletonAssetName(string folder) =>
            folder switch
            {
                "shui_hua" => "sh",
                _ => folder
            };

        static string GetDefaultAnimation(string folder) =>
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

        static Material GetGraphicMaterial(string folder)
        {
            var mat = Resources.Load<Material>($"Spine/{folder}/{GetSkeletonAssetName(folder)}_Material");
            if (mat != null && IsValidSpineShader(mat.shader))
            {
                EnsureStraightAlpha(mat);
                return mat;
            }

            var shader = Shader.Find("Spine/SkeletonGraphic") ?? Shader.Find("UI/Default");
            mat = new Material(shader);
            EnsureStraightAlpha(mat);
            return mat;
        }

        static bool IsValidSpineShader(Shader shader) =>
            shader != null && shader.name != "Hidden/InternalErrorShader";

        static void EnsureStraightAlpha(Material mat)
        {
            if (mat == null || !mat.HasProperty("_StraightAlphaInput")) return;
            mat.SetInt("_StraightAlphaInput", 1);
            mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
        }
    }
}
