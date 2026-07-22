using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    [DisallowMultipleComponent]
    public class WithdrawMissionStateItem : MonoBehaviour
    {
        private GameObject finish;
        private GameObject unfinished;
        private Text countText;

        void Awake()
        {
            CacheReferences();
        }

        public void Initialize()
        {
            CacheReferences();
        }

        public void SetFinished(bool isFinished)
        {
            CacheReferences();

            if (finish != null)
                finish.SetActive(isFinished);

            if (unfinished != null)
                unfinished.SetActive(!isFinished);
        }

        public void SetCount(int count)
        {
            CacheReferences();

            if (countText != null)
                countText.text = count.ToString();
        }

        public void SetState(int count, bool isFinished)
        {
            CacheReferences();
            SetCount(count);
            SetFinished(isFinished);
        }

        void CacheReferences()
        {
            if (finish == null)
                finish = FindChildUtility.FindChild(gameObject, "Finish");

            if (unfinished == null)
                unfinished = FindChildUtility.FindChild(gameObject, "Unfinished");

            if (countText == null)
            {
                GameObject countObj = FindChildUtility.FindChild(gameObject, "Count");
                countText = countObj != null ? countObj.GetComponent<Text>() : null;
            }
        }
    }
}
