namespace WZSDK
{
    using UnityEngine;
    using UnityEngine.UI;
    
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [Header("文本多语言配置")]
        public string textId;
    
        private Text textComponent;
    
        void Start()
        {
            textComponent = GetComponent<Text>();
    
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += UpdateText;
            }
    
            UpdateText();
        }
    
        void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
            }
        }
    
        void UpdateText()
        {
            if (textComponent != null && !string.IsNullOrEmpty(textId))
            {
                textComponent.text = LocalizationManager.Instance.GetText(textId);
            }
        }
    
        public void SetTextId(string newTextId)
        {
            textId = newTextId;
            UpdateText();
        }
    }
}
