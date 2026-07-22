namespace WZSDK
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using DG.Tweening;
    
    public class SplashScreen : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
    
        // Start is called before the first frame update
        void Start()
        {
           
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.5f).SetDelay(1.0f).SetEase(Ease.Linear)
               .OnComplete(() => {
    
                   canvasGroup.alpha = 0.0f;
                   canvasGroup.interactable = false;
                   canvasGroup.blocksRaycasts = false;
                   
               });
        }
    
        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
