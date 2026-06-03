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
            Color? tint = null, float? playDuration = null, string skinName = null)
        {
            if (host == null || !SupportedFolders.Contains(folder)) return false;

            var skeletonData = GetSkeletonData(folder);
            if (!IsSkeletonAssetUsable(skeletonData)) return false;

            SpineRunner.Instance.StartCoroutine(PlayGraphic(host, skeletonData, folder, animationName, loop, onComplete, tint,
                playDuration, skinName));
            return true;
        }

        static IEnumerator PlayGraphic(Transform host, SkeletonDataAsset skeletonData, string folder, string animationName,
            bool loop, Action onComplete, Color? tint, float? playDuration, string skinName)
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

            TrySetSkin(graphic, folder, skinName);
            if (tint.HasValue)
                graphic.color = tint.Value;
            ApplyGraphicLayout(graphic, rt, folder);

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

        static void TrySetSkin(SkeletonGraphic graphic, string folder, string skinName)
        {
            if (graphic?.Skeleton == null) return;
            if (string.IsNullOrEmpty(skinName))
            {
                skinName = folder switch
                {
                    "shui" => "shang",
                    _ => null
                };
            }

            if (string.IsNullOrEmpty(skinName)) return;
            var skin = graphic.Skeleton.Data.FindSkin(skinName);
            if (skin == null) return;
            graphic.Skeleton.SetSkin(skin);
            graphic.Skeleton.SetSlotsToSetupPose();
        }

        static bool IsSkeletonAssetUsable(SkeletonDataAsset asset)
        {
            if (asset == null) return false;
            try
            {
                return asset.GetSkeletonData(false) != null;
            }
            catch
            {
                asset.Clear();
                return false;
            }
        }

        static bool IsAtlasAssetReady(AtlasAssetBase atlasAsset)
        {
            if (atlasAsset == null) return false;
            if (atlasAsset is SpineAtlasAsset spineAtlas)
                return spineAtlas.atlasFile != null;
            return true;
        }

        static SkeletonDataAsset GetSkeletonData(string folder)
        {
            if (SkeletonCache.TryGetValue(folder, out var cached))
            {
                if (IsSkeletonAssetUsable(cached))
                    return cached;
                SkeletonCache.Remove(folder);
            }

            var assetName = GetSkeletonAssetName(folder);
            var imported = Resources.Load<SkeletonDataAsset>($"Spine/{folder}/{assetName}_SkeletonData");
            if (imported != null && HasReadyAtlasAssets(imported) && IsSkeletonAssetUsable(imported))
            {
                SkeletonCache[folder] = imported;
                return imported;
            }

            if (imported != null)
                imported.Clear();

            var runtime = CreateRuntimeSkeletonData(folder);
            if (runtime != null && IsSkeletonAssetUsable(runtime))
                SkeletonCache[folder] = runtime;
            return runtime;
        }

        static bool HasReadyAtlasAssets(SkeletonDataAsset asset)
        {
            if (asset?.atlasAssets == null || asset.atlasAssets.Length == 0) return false;
            foreach (var atlas in asset.atlasAssets)
            {
                if (!IsAtlasAssetReady(atlas))
                    return false;
            }

            return true;
        }

        static SkeletonDataAsset CreateRuntimeSkeletonData(string folder)
        {
            var jsonName = GetSkeletonAssetName(folder);
            var json = LoadTextAsset($"Spine/{folder}", jsonName);
            if (json == null || SpineRuntimeCompat.IsSpine38Json(json.text))
                return null;

            var importedAtlas = Resources.Load<SpineAtlasAsset>($"Spine/{folder}/{jsonName}_Atlas");
            if (IsAtlasAssetReady(importedAtlas))
                return SkeletonDataAsset.CreateRuntimeInstance(json, importedAtlas, true, 0.01f);

            var atlasText = LoadTextAsset($"Spine/{folder}", jsonName + ".atlas.txt")
                            ?? LoadTextAsset($"Spine/{folder}", jsonName + ".atlas");
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
                "he_cheng_2" => "guang",
                "shui" => "huang",
                "wht" => "animation2",
                "dai_zi" => "zhuang",
                _ => "animation"
            };

        static void ApplyGraphicLayout(SkeletonGraphic graphic, RectTransform rt, string folder)
        {
            if (graphic == null || rt == null) return;
            try
            {
                graphic.MatchRectTransformWithBounds();
            }
            catch
            {
                rt.sizeDelta = folder switch
                {
                    "xuan_zhong" => new Vector2(160f, 80f),
                    "bao_xing" => new Vector2(280f, 360f),
                    "dai_zi" => new Vector2(140f, 260f),
                    _ => new Vector2(120f, 120f)
                };
            }
        }

        static Material GetGraphicMaterial(string folder)
        {
            var jsonName = GetSkeletonAssetName(folder);
            var mat = Resources.Load<Material>($"Spine/{folder}/{jsonName}_Material");
            if (mat != null && IsValidGraphicMaterial(mat))
            {
                EnsureStraightAlpha(mat);
                BindMaterialTexture(mat, folder, jsonName);
                return mat;
            }

            var shader = Shader.Find("Spine/SkeletonGraphic") ?? Shader.Find("UI/Default");
            mat = new Material(shader);
            BindMaterialTexture(mat, folder, jsonName);
            EnsureStraightAlpha(mat);
            return mat;
        }

        static void BindMaterialTexture(Material mat, string folder, string jsonName)
        {
            if (mat == null || !mat.HasProperty("_MainTex")) return;
            var tex = Resources.Load<Texture2D>($"Spine/{folder}/{jsonName}");
            if (tex == null)
            {
                var sp = GameResourceLoader.LoadSprite($"Spine/{folder}/{jsonName}");
                tex = sp != null ? sp.texture : null;
            }

            if (tex != null)
                mat.mainTexture = tex;
        }

        static bool IsValidGraphicMaterial(Material mat)
        {
            if (mat == null || mat.shader == null) return false;
            var name = mat.shader.name;
            return name != "Hidden/InternalErrorShader"
                   && !name.Contains("HiddenPass", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsValidSpineShader(Shader shader)
        {
            if (shader == null) return false;
            var name = shader.name;
            return name != "Hidden/InternalErrorShader"
                   && !name.Contains("HiddenPass", StringComparison.OrdinalIgnoreCase);
        }

        static void EnsureStraightAlpha(Material mat)
        {
            if (mat == null || !mat.HasProperty("_StraightAlphaInput")) return;
            mat.SetInt("_StraightAlphaInput", 1);
            mat.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
        }
    }
}
