using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LuckyItemProgress : MonoBehaviour
{
    private GameObject luckyRewardIcon;
    private GameObject luckyIcon;
    private GameObject unfinished;
    private Text count;
    private GameObject finish;
    private bool rewardIconEnabledByConfig;
    private bool isFinishedState;
    private bool isLuckyLevelState;

    void Awake()
    {
        CacheReferences();
    }

    void Start()
    {
        CacheReferences();
    }

    public void Initialize()
    {
        CacheReferences();
    }

    public void SetCount(int level)
    {
        CacheReferences();

        if (count != null)
            count.text = level.ToString();
    }

    public void SetFinished(bool isFinished)
    {
        CacheReferences();
        isFinishedState = isFinished;

        if (unfinished != null)
            unfinished.SetActive(!isFinished && !isLuckyLevelState);

        if (luckyIcon != null)
            luckyIcon.SetActive(!isFinished && isLuckyLevelState);

        if (finish != null)
            finish.SetActive(isFinished);

        UpdateLuckyRewardIconState();
    }

    public void SetState(int level, bool isFinished)
    {
        SetState(level, isFinished, false);
    }

    public void SetState(int level, bool isFinished, bool isLuckyLevel)
    {
        CacheReferences();
        isLuckyLevelState = isLuckyLevel;
        SetCount(level);
        SetFinished(isFinished);
    }

    public void SetLuckyRewardIconVisible(bool isVisible)
    {
        CacheReferences();
        rewardIconEnabledByConfig = isVisible;

        UpdateLuckyRewardIconState();
    }

    private void UpdateLuckyRewardIconState()
    {
        if (luckyRewardIcon != null)
            luckyRewardIcon.SetActive(rewardIconEnabledByConfig && !isFinishedState);
    }

    private void CacheReferences()
    {
        if (luckyRewardIcon == null)
            luckyRewardIcon = FindChildUtility.FindChild(gameObject, "LuckyRewardIcon");

        if (luckyIcon == null)
            luckyIcon = FindChildUtility.FindChild(gameObject, "LuckyIcon");

        if (unfinished == null)
            unfinished = FindChildUtility.FindChild(gameObject, "Unfinished");

        if (count == null)
            count = FindChildUtility.FindChild<Text>(gameObject, "Count");

        if (finish == null)
            finish = FindChildUtility.FindChild(gameObject, "Finish");
    }
}
