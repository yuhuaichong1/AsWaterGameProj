using System;
using System.Collections;
using UnityEngine;
using AsGame.Core;

namespace AsGame.Spine
{
    /// <summary>未安装 spine-unity 时的占位：缩放脉冲代替特效。</summary>
    public class SpinePlayerStub : ISpinePlayer
    {
        public void Play(Transform host, string skeletonResourcePath, string animationName, bool loop, Action onComplete = null,
            Color? tint = null, float? duration = null)
        {
            if (host == null) return;
            SpineRunner.Instance.StartCoroutine(Pulse(host, onComplete));
        }

        public void SetSkin(Transform host, string skinName) { }

        public void Clear(Transform host)
        {
            if (host == null) return;
            host.localScale = Vector3.one;
        }

        static IEnumerator Pulse(Transform host, Action onComplete)
        {
            if (host == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            yield return TweenHelper.ToFloatWhile(host, 1f, 1.25f, 0.15f, s => host.localScale = Vector3.one * s);
            if (host == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            yield return TweenHelper.ToFloatWhile(host, 1.25f, 1f, 0.15f, s => host.localScale = Vector3.one * s);
            onComplete?.Invoke();
        }
    }

    class SpineRunner : MonoBehaviour
    {
        public static SpineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SpineRunner", typeof(SpineRunner));
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _instance = go.GetComponent<SpineRunner>();
                }

                return _instance;
            }
        }

        static SpineRunner _instance;
    }

    public static class SpineService
    {
        static ISpinePlayer _player = new SpriteFxPlayer();

        public static void SetPlayer(ISpinePlayer player) => _player = player;

        public static void PlayEffect(Transform parent, Vector3 localPos, string folder, string anim, bool loop = false,
            Action onComplete = null, Color? tint = null, float? duration = null)
        {
            var fx = new GameObject("SpineFx_" + folder, typeof(RectTransform));
            fx.transform.SetParent(parent, false);
            fx.transform.localPosition = localPos;
            var destroyAfterPlay = duration.HasValue || !loop;
            _player.Play(fx.transform, "Spine/" + folder, anim, loop, () =>
            {
                onComplete?.Invoke();
                if (destroyAfterPlay && fx != null) UnityEngine.Object.Destroy(fx);
            }, tint, duration);
        }

        public static void ClearEffects(Transform parent)
        {
            if (parent == null) return;
            _player.Clear(parent);
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name.StartsWith("SpineFx_"))
                    UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
