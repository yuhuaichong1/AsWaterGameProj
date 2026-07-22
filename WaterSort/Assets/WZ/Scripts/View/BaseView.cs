using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
namespace WZSDK
{
    public abstract class BaseView : UIManagedViewBase
    {
        public CanvasGroup canvasGroup;

        [HideInInspector]
        public bool isShow;

        // 可选的动画参数
        [Header("Animation Settings")]
        public bool useAnimations = true;



        public abstract void Start();
        public abstract void Update();
        public abstract void InitView();

        public override bool IsShow()
        {
            return isShow;
        }

        public override void InitUi()
        {
        }

        public override void InitData()
        {
            MarkInitialized();
        }

        private CanvasGroup GetCanvasGroup()
        {
            if (canvasGroup != null)
                return canvasGroup;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                Debug.LogWarning($"{GetType().Name} on '{name}' had no CanvasGroup assigned; one was added automatically.", this);
            }

            return canvasGroup;
        }



        private bool ShouldPlayAnimation()
        {

            if (!useAnimations) return false;

            if (UIManager.instance == null)
            {
                Debug.LogWarning("UIManager.instance is null, defaulting to play animation");
                return true;
            }
            string viewName = this.gameObject.name;
            if (viewName.Contains("(Clone)"))
            {
                viewName = viewName.Replace("(Clone)", "");
            }
            bool isInNoAnimationList = UIManager.noAnimationViewNames.Contains(viewName);
            return !isInNoAnimationList;
        }

        public virtual void ShowView()
        {
            ShowView(null);
        }

        public virtual void ShowView(params object[] args)
        {
            CanvasGroup viewCanvasGroup = GetCanvasGroup();

            if (args != null && args.Length > 0)
            {
                HandleViewArgs(args);
            }


            if (ShouldPlayAnimation())
            {
                var rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.DOKill(true);
                    rectTransform.localScale = Vector3.zero;
                    viewCanvasGroup.alpha = 1.0f;
                    viewCanvasGroup.interactable = true;
                }

                ShowAnim(rectTransform, () =>
                {
                    viewCanvasGroup.blocksRaycasts = true;
                    isShow = true;
                });
            }
            else
            {
                viewCanvasGroup.alpha = 1.0f;
                viewCanvasGroup.interactable = true;
                viewCanvasGroup.blocksRaycasts = true;
                isShow = true;
            }

            MarkInitialized();
            InitView();
        }

        public virtual void HideView()
        {
            CanvasGroup viewCanvasGroup = GetCanvasGroup();

            if (ShouldPlayAnimation())
            {
                HideAnim(transform as RectTransform, () =>
                {
                    viewCanvasGroup.alpha = 0.0f;
                    viewCanvasGroup.interactable = false;
                    viewCanvasGroup.blocksRaycasts = false;
                    isShow = false;
                });
            }
            else
            {
                viewCanvasGroup.alpha = 0.0f;
                viewCanvasGroup.interactable = false;
                viewCanvasGroup.blocksRaycasts = false;
                isShow = false;
            }
        }

        protected virtual void HandleViewArgs(object[] args)
        {
            // 子类重写处理参数逻辑
        }

        public override void Open(params object[] args)
        {
            ShowView(args);
        }

        public override void Close(params object[] args)
        {
            HideView();
        }

        public override void SetVisible(bool value)
        {
            gameObject.SetActive(value);
            isShow = value;
        }

        #region 动画方法

        /// <summary>
        /// 显示动画 - 从0缩放到1
        /// </summary>
        protected void ShowAnim(RectTransform target, Action successAction = null)
        {
            if (target == null)
            {
                successAction?.Invoke();
                return;
            }

            target.localScale = Vector3.zero;
            target.DOScale(Vector3.one, GameDefines.ShowAnimTime)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    successAction?.Invoke();
                });
        }

        /// <summary>
        /// 隐藏动画 - 从1缩放到0
        /// </summary>
        protected void HideAnim(RectTransform target, Action successAction = null)
        {
            if (target == null)
            {
                successAction?.Invoke();
                return;
            }

            target.localScale = Vector3.one;
            target.DOScale(Vector3.zero, GameDefines.ShowAnimTime)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    target.localScale = Vector3.zero;
                    successAction?.Invoke();
                });
        }
        #endregion
    }
}
