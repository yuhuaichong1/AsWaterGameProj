using System;
using System.Collections;
using UnityEngine;
using AsGame.Core;
using AsGame.Events;
using XrCode;

namespace AsGame.UI
{
    public abstract class BasePopupView : MonoBehaviour
    {
        protected PopupContext Context;
        protected RectTransform ContentRoot;
        protected CanvasGroup Blocker;

        public virtual void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            Context = ctx;
            ContentRoot = content;
            Blocker = blocker;
            EventBus.Publish(GameEvents.PopupShow, ctx.Type);
            // 延后一帧，让子类在 base.Setup 之后写完 UI 再播淡入
            StartCoroutine(ShowAnimationDeferred());
        }

        IEnumerator ShowAnimationDeferred()
        {
            yield return null;
            yield return ShowAnimation();
        }

        protected IEnumerator ShowAnimation()
        {
            if (ContentRoot == null) yield break;
            ContentRoot.localScale = Vector3.one * 0.85f;
            var cg = GetOrAddContentCanvasGroup();
            if (cg == null) yield break;

            cg.alpha = 0f;
            yield return TweenHelper.ToFloat(0f, 1f, 0.25f, a =>
            {
                var group = GetOrAddContentCanvasGroup();
                if (group != null) group.alpha = a;
            });
            yield return TweenHelper.ToVector3(ContentRoot.localScale, Vector3.one, 0.2f,
                s => ContentRoot.localScale = s);
        }

        CanvasGroup GetOrAddContentCanvasGroup()
        {
            if (ContentRoot == null) return null;
            var go = ContentRoot.gameObject;
            if (!go.TryGetComponent<CanvasGroup>(out var cg))
                cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        public void Close()
        {
            EventBus.Publish(GameEvents.PopupClose, Context?.Type);
            Context?.OnClose?.Invoke();
            Destroy(gameObject);
        }

        public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
