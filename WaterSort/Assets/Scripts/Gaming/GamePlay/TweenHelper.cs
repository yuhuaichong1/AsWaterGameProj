using System;
using System.Collections;
using UnityEngine;

namespace XrCode
{
    /// <summary>替代 Cocos cc.tween 的轻量补间，避免瞬时跳变。</summary>
    public static class TweenHelper
    {
        public static IEnumerator ToFloat(float from, float to, float duration, Action<float> onUpdate, Action onComplete = null)
        {
            duration = Mathf.Clamp(duration, GameConstants.MinAnimDuration, GameConstants.MaxAnimDuration);
            if (duration <= 0f)
            {
                onUpdate?.Invoke(to);
                onComplete?.Invoke();
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                onUpdate?.Invoke(Mathf.Lerp(from, to, EaseOutSine(t)));
                yield return null;
            }

            onUpdate?.Invoke(to);
            onComplete?.Invoke();
        }

        /// <summary>目标被销毁时提前结束，避免对已销毁 Transform 写属性。</summary>
        public static IEnumerator ToFloatWhile(UnityEngine.Object target, float from, float to, float duration,
            Action<float> onUpdate, Action onComplete = null)
        {
            duration = Mathf.Clamp(duration, GameConstants.MinAnimDuration, GameConstants.MaxAnimDuration);
            if (target == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            if (duration <= 0f)
            {
                if (target != null)
                    onUpdate?.Invoke(to);
                onComplete?.Invoke();
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null)
                    yield break;

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                onUpdate?.Invoke(Mathf.Lerp(from, to, EaseOutSine(t)));
                yield return null;
            }

            if (target != null)
                onUpdate?.Invoke(to);
            onComplete?.Invoke();
        }

        public static IEnumerator MoveLocal(Transform target, Vector3 end, float duration, Action onComplete = null)
        {
            var start = target.localPosition;
            yield return ToVector3(start, end, duration, v => target.localPosition = v, onComplete);
        }

        public static IEnumerator MoveWorld(Transform target, Vector3 end, float duration, Action onComplete = null)
        {
            var start = target.position;
            yield return ToVector3(start, end, duration, v => target.position = v, onComplete);
        }

        public static IEnumerator ToVector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate, Action onComplete = null)
        {
            duration = Mathf.Clamp(duration, GameConstants.MinAnimDuration, GameConstants.MaxAnimDuration);
            if (duration <= 0f)
            {
                onUpdate?.Invoke(to);
                onComplete?.Invoke();
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                onUpdate?.Invoke(Vector3.Lerp(from, to, EaseOutSine(t)));
                yield return null;
            }

            onUpdate?.Invoke(to);
            onComplete?.Invoke();
        }

        public static IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
        {
            if (group == null) yield break;
            yield return ToFloat(group.alpha, targetAlpha, duration, a => group.alpha = a);
        }

        public static IEnumerator Delay(float seconds, Action onComplete)
        {
            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);
            onComplete?.Invoke();
        }

        public static IEnumerator Sequence(params IEnumerator[] steps)
        {
            foreach (var step in steps)
            {
                if (step != null)
                    yield return step;
            }
        }

        static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI * 0.5f);
    }
}
