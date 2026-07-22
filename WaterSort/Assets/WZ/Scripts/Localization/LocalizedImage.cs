namespace WZSDK
{
    using UnityEngine;
    using UnityEngine.UI;
    
    [RequireComponent(typeof(Image))]
    public class LocalizedImage : MonoBehaviour
    {
        [Header("图片多语言配置")]
        public string imageId;
    
        private Image imageComponent;
    
        void Start()
        {
            imageComponent = GetComponent<Image>();
    
            if (ImageLocalizationManager.Instance != null)
            {
                ImageLocalizationManager.Instance.OnImageLanguageChanged += UpdateImage;
            }
    
            UpdateImage();
        }
    
        void OnDestroy()
        {
            if (ImageLocalizationManager.Instance != null)
            {
                ImageLocalizationManager.Instance.OnImageLanguageChanged -= UpdateImage;
            }
        }
    
        public void UpdateImage()
        {
            if (imageComponent != null && !string.IsNullOrEmpty(imageId))
            {
                Sprite sprite = ImageLocalizationManager.Instance.GetSprite(imageId);
                if (sprite != null)
                {
                    imageComponent.sprite = sprite;
                }
            }
        }
    
        public void SetImageId(string newImageId)
        {
            imageId = newImageId;
            UpdateImage();
        }
    }
}
