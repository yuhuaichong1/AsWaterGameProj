namespace WZSDK
{
    using UnityEngine;
    using DG.Tweening;
    using System.Collections;
    
    public class ShakeEffect : MonoBehaviour
    {
        [Header("摇晃设置")]
        public float swingAngle = 15f;             // 摇晃角度
        public Ease normalEase = Ease.InOutSine;   // 正常摇晃的缓动类型
        public Ease fastEase = Ease.InOutSine;     // 快速摇晃的缓动类型
    
        [Header("时间设置")]
        public float normalDuration = 0.3f;        // 正常左右一次的时间
        public float fastDuration = 0.05f;         // 快速摇晃单次时间
        public float fastIntensity = 0.8f;         // 快速摇晃强度
    
        [Header("自动摇晃")]
        public bool autoSwing = true;              // 是否自动摇晃
        [Tooltip("动画完成后停顿的时间")]
        public float pauseAfterSwing = 1f;         // 摇晃后停顿时间
    
        [Header("摇晃模式")]
        public SwingMode swingMode = SwingMode.Rotation; // 摇晃方式
    
        private Vector3 originalPosition;           // 原始位置
        private Quaternion originalRotation;        // 原始旋转
        private Sequence swingSequence;             // 摇晃序列
        private Coroutine autoSwingCoroutine;       // 自动摇晃协程
    
        public enum SwingMode
        {
            Rotation,    // 旋转摇晃（推荐）
            Position     // 位置摇晃
        }
    
        private void OnEnable()
        {
            // 保存原始状态
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
    
            // 如果开启自动摇晃，开始循环摇晃
            if (autoSwing)
            {
                StartAutoSwing();
            }
        }
    
        private void OnDisable()
        {
            // 停止自动摇晃协程
            StopAutoSwing();
    
            // 停止当前动画
            if (swingSequence != null && swingSequence.active)
            {
                swingSequence.Kill();
                swingSequence = null;
            }
    
            // 重置位置
            ResetToOriginal();
        }
    
        private void OnDestroy()
        {
            // 清理 DOTween 序列
            if (swingSequence != null && swingSequence.active)
            {
                swingSequence.Kill();
            }
            StopAutoSwing();
        }
    
        /// <summary>
        /// 开始自动摇晃
        /// </summary>
        public void StartAutoSwing()
        {
            StopAutoSwing();
            autoSwingCoroutine = StartCoroutine(AutoSwingRoutine());
        }
    
        /// <summary>
        /// 停止自动摇晃
        /// </summary>
        public void StopAutoSwing()
        {
            if (autoSwingCoroutine != null)
            {
                StopCoroutine(autoSwingCoroutine);
                autoSwingCoroutine = null;
            }
        }
    
        /// <summary>
        /// 自动摇晃协程
        /// </summary>
        private IEnumerator AutoSwingRoutine()
        {
            while (autoSwing)
            {
                // 播放一次完整的摇晃动画
                StartSwing();
    
                // 等待动画完全结束
                float totalAnimationTime = CalculateAnimationTime();
                yield return new WaitForSeconds(totalAnimationTime);
    
                // 动画结束后停顿
                yield return new WaitForSeconds(pauseAfterSwing);
            }
        }
    
        /// <summary>
        /// 计算动画的总时间
        /// </summary>
        private float CalculateAnimationTime()
        {
            float normalTime = normalDuration * 0.8f; // 稍微加快
            float fastTime = fastDuration * 4f;
            float returnTime = fastDuration * 0.5f;
    
            return normalTime + fastTime + returnTime;
        }
    
        /// <summary>
        /// 开始摇晃效果：左右一次 + 快速四次
        /// </summary>
        public void StartSwing()
        {
            // 如果物体未激活，不播放动画
            if (!gameObject.activeInHierarchy)
                return;
    
            // 安全地清理旧的序列
            if (swingSequence != null)
            {
                if (swingSequence.active)
                {
                    swingSequence.Kill();
                }
                swingSequence = null;
            }
    
            // 创建新序列
            swingSequence = DOTween.Sequence();
    
            if (swingMode == SwingMode.Rotation)
            {
                StartRotationSwing();
            }
            else
            {
                StartPositionSwing();
            }
    
            swingSequence.OnComplete(() => {
                ResetToOriginal();
            });
        }
    
        /// <summary>
        /// 旋转摇晃方式：左右一次 + 快速四次
        /// </summary>
        private void StartRotationSwing()
        {
            float quickAngle = swingAngle * fastIntensity;
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, swingAngle),
                normalDuration * 0.4f).SetEase(Ease.OutSine));
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -swingAngle),
                normalDuration * 0.4f).SetEase(Ease.InOutSine));
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, quickAngle),
                fastDuration).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -quickAngle),
                fastDuration).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, quickAngle * 0.8f),
                fastDuration * 0.9f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -quickAngle * 0.8f),
                fastDuration * 0.9f).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, quickAngle * 0.6f),
                fastDuration * 0.8f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -quickAngle * 0.6f),
                fastDuration * 0.8f).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, quickAngle * 0.4f),
                fastDuration * 0.7f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -quickAngle * 0.4f),
                fastDuration * 0.7f).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalRotate(new Vector3(0, 0, 0),
                fastDuration * 0.5f).SetEase(Ease.OutSine));
        }
    
        /// <summary>
        /// 位置摇晃方式：左右一次 + 快速四次
        /// </summary>
        private void StartPositionSwing()
        {
            float moveDistance = swingAngle * 0.1f;
            float quickMove = moveDistance * fastIntensity;
    
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x + moveDistance,
                normalDuration * 0.4f).SetEase(Ease.OutSine));
    
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x - moveDistance,
                normalDuration * 0.4f).SetEase(Ease.InOutSine));
    
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x + quickMove,
                fastDuration).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x - quickMove,
                fastDuration).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x + quickMove * 0.8f,
                fastDuration * 0.9f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x - quickMove * 0.8f,
                fastDuration * 0.9f).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x + quickMove * 0.6f,
                fastDuration * 0.8f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x - quickMove * 0.6f,
                fastDuration * 0.8f).SetEase(fastEase));
    
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x + quickMove * 0.4f,
                fastDuration * 0.7f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x - quickMove * 0.4f,
                fastDuration * 0.7f).SetEase(fastEase));
            swingSequence.Append(transform.DOLocalMoveX(originalPosition.x,
                fastDuration * 0.5f).SetEase(Ease.OutSine));
        }
    
        /// <summary>
        /// 立即停止摇晃
        /// </summary>
        public void StopSwing()
        {
            if (swingSequence != null && swingSequence.active)
            {
                swingSequence.Kill();
            }
            swingSequence = null;
            ResetToOriginal();
        }
    
        /// <summary>
        /// 重置到原始状态
        /// </summary>
        private void ResetToOriginal()
        {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
        }
    
        // 在编辑器中可视化摇晃角度
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 leftDir = Quaternion.Euler(0, 0, swingAngle) * Vector3.up;
            Vector3 rightDir = Quaternion.Euler(0, 0, -swingAngle) * Vector3.up;
    
            Gizmos.DrawLine(transform.position, transform.position + leftDir * 1f);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * 1f);
        }
    }
}
