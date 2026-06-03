using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;

namespace AsGame.Spine
{
    /// <summary>无 spine-unity 时用 Resources 里的 Spine 图集 PNG 播放简易特效。</summary>
    public class SpriteFxPlayer : ISpinePlayer
    {
        static readonly Dictionary<string, string> SpriteByFolder = new()
        {
            { "shui", "Spine/shui/shui" },
            { "shui_hua", "Spine/shui_hua/sh" },
            { "xuan_zhong", "Spine/xuan_zhong/xuan_zhong" },
            { "bao_xing", "Spine/bao_xing/bao_xing" },
            { "he_cheng_1", "Spine/he_cheng_1/he_cheng_1" },
            { "he_cheng_2", "Spine/he_cheng_2/he_cheng_2" },
            { "sheng_cheng", "Spine/sheng_cheng/sheng_cheng" },
        };

        readonly SpinePlayerStub _fallback = new();

        public void Play(Transform host, string skeletonResourcePath, string animationName, bool loop,
            Action onComplete = null, Color? tint = null, float? duration = null, string skinName = null)
        {
            if (host == null)
            {
                onComplete?.Invoke();
                return;
            }

            var folder = skeletonResourcePath;
            if (folder.StartsWith("Spine/"))
                folder = folder.Substring("Spine/".Length);

            if (SpineGraphicFxPlayer.TryPlay(host, folder, animationName, loop, onComplete, tint, duration, skinName))
                return;

            if (SpineAtlasPlayer.TryPlay(host, folder, animationName, loop, onComplete, tint, duration))
                return;

            if (SpriteByFolder.TryGetValue(folder, out var resPath) &&
                GameResourceLoader.LoadSprite(resPath) != null)
            {
                SpineRunner.Instance.StartCoroutine(PlaySpriteFx(host, resPath, onComplete));
                return;
            }

            _fallback.Play(host, skeletonResourcePath, animationName, loop, onComplete);
        }

        public void SetSkin(Transform host, string skinName) { }

        public void Clear(Transform host)
        {
            if (host == null) return;
            for (var i = host.childCount - 1; i >= 0; i--)
            {
                var child = host.GetChild(i);
                if (child.name.StartsWith("SpineFx_"))
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        static IEnumerator PlaySpriteFx(Transform host, string resourcePath, Action onComplete)
        {
            var sp = GameResourceLoader.LoadSprite(resourcePath);
            if (sp == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var go = new GameObject("SpineFx_Sprite", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one * 0.3f;
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.SetNativeSize();
            img.raycastTarget = false;
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0.9f;

            yield return TweenHelper.ToFloatWhile(go, 0.3f, 1.2f, 0.35f, s => rt.localScale = Vector3.one * s);
            yield return TweenHelper.ToFloatWhile(go, 1.2f, 0.5f, 0.25f, s =>
            {
                rt.localScale = Vector3.one * s;
                cg.alpha = s / 1.2f;
            });

            if (go != null)
                UnityEngine.Object.Destroy(go);
            onComplete?.Invoke();
        }
    }
}
