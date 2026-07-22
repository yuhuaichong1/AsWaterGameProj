namespace WZSDK
{
    using UnityEngine;
    using UnityEngine.UI;
    
    public class NetworkErrorTips : MonoBehaviour
    {
        private const string NetworkErrorTextKey = "3083";
        private static readonly string[] DotSuffixes = { ".", "..", "..." };

        public Text text;
        private int count = 0;
        private bool isEnd;
    
        private void Start()
        {
            isEnd = false;
            InvokeRepeating("ChangeContent", 0f, 0.5f);
        }
    
        public void ChangeContent()
        {
            if (isEnd) return;

            string baseText = GetLocalizedNetworkErrorText();
            string suffix = DotSuffixes[count % DotSuffixes.Length];
            if (text != null)
                text.text = baseText + suffix;

            count++;
        }

        public void CanelShow()
        {
            isEnd = true;
            if (text != null)
                text.text = string.Empty;

            CancelInvoke(nameof(ChangeContent));
        }

        private string GetLocalizedNetworkErrorText()
        {
            if (LocalizationManager.Instance == null)
                return "Network issue detected. Please wait a moment";

            string localizedText = LocalizationManager.Instance.GetText(NetworkErrorTextKey);
            return string.IsNullOrEmpty(localizedText) || localizedText == NetworkErrorTextKey
                ? "Network issue detected. Please wait a moment"
                : localizedText;
        }
    }
}
