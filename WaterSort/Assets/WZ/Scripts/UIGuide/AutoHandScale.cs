namespace WZSDK
{
    using DG.Tweening;
    using UnityEngine;
    
    public class AutoHandScale : MonoBehaviour
    {
        public bool ifAwake = true;//是否在一开始就启用
        public float STime = 1f;
        public float Size = 1.25f;
    
        void Start()
        {
            transform.localScale = Vector3.one;
    
            if (ifAwake)
            {
                StartScale();
            }
        }
    
        private void StartScale()
        {
            transform.DOScale(Size, STime / 2)
                .SetLoops(-1, LoopType.Yoyo);
    
        }
    
        public void Reset()
        {
            //transform.localScale = Vector3.one;
            //StartScale();
        }
    }
}
