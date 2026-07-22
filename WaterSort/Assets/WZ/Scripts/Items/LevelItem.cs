using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class LevelItem : MonoBehaviour
    {
        public GameObject Ng1;
        public GameObject Ng2;
        public Text WLevelText;
        public GameObject tips;
        public GameObject Completed;
        public GameObject SmallLevelGroup;
        public Text SmallLevelTxt;

        public void SetLevel(int level, int currentLevel)
        {
            SetAllFalse();

            bool shouldShowTips = ShouldShowTips(level, currentLevel);

            if (IsLevelCompleted(level, currentLevel))
            {
                if (Completed != null)
                    Completed.SetActive(true);

                if (WLevelText != null)
                    WLevelText.gameObject.SetActive(false);

                if (SmallLevelGroup != null)
                    SmallLevelGroup.SetActive(false);

                return;
            }

            int start = GameDefines.SmallLevelGroupStart;
            int end = GameDefines.SmallLevelGroupEnd;
            int baseDisplayLevel = start - 1;
            int totalSmallLevels = 10;

            if (level == currentLevel)
            {
                if (currentLevel >= start && currentLevel <= end)
                {
                    if (SmallLevelGroup != null)
                    {
                        SmallLevelGroup.SetActive(true);

                        if (SmallLevelTxt != null)
                        {
                            int currentIndex = currentLevel - start + 2;
                            string displayText = string.Format("{0}/{1}", currentIndex, totalSmallLevels);
                            SmallLevelTxt.text = displayText;
                        }
                    }

                    if (WLevelText != null)
                    {
                        WLevelText.text = string.Format(LocalizationManager.Instance.GetText("1004"), baseDisplayLevel);
                        WLevelText.gameObject.SetActive(true);
                    }

                    if (tips != null)
                        tips.SetActive(true);
                }
                else
                {
                    int displayLevel = GetDisplayLevel(level);
                    if (WLevelText != null)
                    {
                        WLevelText.text = string.Format(LocalizationManager.Instance.GetText("1004"), displayLevel);
                        WLevelText.gameObject.SetActive(true);
                    }
                    if (SmallLevelGroup != null)
                        SmallLevelGroup.SetActive(false);

                    if (tips != null)
                        tips.SetActive(shouldShowTips);
                }

                if (Ng2 != null)
                    Ng2.SetActive(true);
            }
            else
            {
                int displayLevel = GetDisplayLevel(level);

                if (WLevelText != null)
                {
                    WLevelText.text = string.Format(LocalizationManager.Instance.GetText("1004"), displayLevel);
                    WLevelText.gameObject.SetActive(true);
                }

                if (SmallLevelGroup != null)
                    SmallLevelGroup.SetActive(false);

                if (tips != null)
                    tips.SetActive(shouldShowTips);

                if (Ng1 != null)
                    Ng1.SetActive(true);
            }
        }

        private bool ShouldShowTips(int level, int currentLevel)
        {
            int start = GameDefines.SmallLevelGroupStart;
            int end = GameDefines.SmallLevelGroupEnd;

            if (level == 1 || level == 2 || level == 8)
                return true;

            if (currentLevel >= start && currentLevel <= end)
                return true;

            return false;
        }

        private int GetDisplayLevel(int level)
        {
            int start = GameDefines.SmallLevelGroupStart;
            int end = GameDefines.SmallLevelGroupEnd;
            int baseDisplayLevel = start - 1;
            int totalSmallLevels = end - start + 1;

            if (level < start)
            {
                return level;
            }
            else if (level <= end)
            {
                return baseDisplayLevel;
            }
            else
            {
                return level - totalSmallLevels;
            }
        }

        private void SetAllFalse()
        {
            if (Ng1 != null) Ng1.SetActive(false);
            if (Ng2 != null) Ng2.SetActive(false);
            if (Completed != null) Completed.SetActive(false);
            if (SmallLevelGroup != null) SmallLevelGroup.SetActive(false);
            if (tips != null) tips.SetActive(false);
        }

        private bool IsLevelCompleted(int level, int currentLevel)
        {
            return level < currentLevel;
        }
    }
}