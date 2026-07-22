using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    [DisallowMultipleComponent]
    public class WithDrawProcessStateItem : MonoBehaviour
    {
        private Text content1;
        private GameObject line;
        private GameObject wait;
        private GameObject Finsh;
        private string waitingContent = string.Empty;
        private string finishedContent = string.Empty;
        private bool isCompleted;
        private bool useFinishedContent;

        void Awake()
        {
            CacheReferences();
        }

        public void Initialize()
        {
            CacheReferences();
        }

        public void SetContents(string waitingValue, string finishedValue)
        {
            CacheReferences();
            waitingContent = waitingValue ?? string.Empty;
            finishedContent = string.IsNullOrEmpty(finishedValue) ? waitingContent : finishedValue;
            RefreshContent();
        }

        public void SetContent(string value)
        {
            SetContents(value, value);
        }

        public void SetCompleted(bool isCompleted)
        {
            CacheReferences();
            this.isCompleted = isCompleted;
            useFinishedContent = isCompleted;

            if (wait != null)
                wait.SetActive(!isCompleted);

            if (Finsh != null)
                Finsh.SetActive(isCompleted);

            RefreshContent();
        }

        public void SetFinishedContentVisible(bool isVisible)
        {
            useFinishedContent = isVisible;
            RefreshContent();
        }

        public void ResetState()
        {
            SetCompleted(false);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void SetLineVisible(bool isVisible)
        {
            CacheReferences();
            if (line != null)
                line.SetActive(isVisible);
        }

        void CacheReferences()
        {
            if (content1 == null)
                content1 = FindChildUtility.FindChild<Text>(gameObject, "content1");

            if (line == null)
                line = FindChildUtility.FindChild(gameObject, "line");

            if (wait == null)
                wait = FindChildUtility.FindChild(gameObject, "wait");

            if (Finsh == null)
                Finsh = FindChildUtility.FindChild(gameObject, "Finsh");
        }

        private void RefreshContent()
        {
            if (content1 == null)
                return;

            content1.text = useFinishedContent ? finishedContent : waitingContent;
        }
    }
}
