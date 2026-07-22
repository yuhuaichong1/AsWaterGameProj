using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LotteryPanel : MonoBehaviour
{
    private const int VisibleCenterOffset = 2;

    public event Action<LotteryPanel, bool, int> RollCompleted;
    public event Action<LotteryPanel, bool> RollStarted;

    public List<Text> textList;
    public List<Text> textList1;
    public List<Text> textList2;
    public List<Text> textList3;
    public float H;

    public AnimationCurve line;
    public Text goldText;
    public Image icon;
    public int randomInt;
    [SerializeField] private float digitStaggerDelay = 0.5f;
    [SerializeField] private float stepDuration = 0.01f;
    [SerializeField] private float baseRollCount = 100f;
    [SerializeField] private int settleStepCount = 20;

    public List<int> currentList = new List<int>();

    private readonly List<List<Text>> textLists = new List<List<Text>>();
    private readonly List<int> randomList = new List<int>();
    private readonly List<Tween> activeTweens = new List<Tween>();

    private Coroutine initCoroutine;
    private bool currentRollIsRewardRoll;
    private int currentTargetValue;

    private void OnDisable()
    {
        StopRolling();
    }

    private void OnDestroy()
    {
        StopRolling();
    }

    public void SetRandomInt(int value)
    {
        randomInt = Mathf.Max(0, value);
    }

    public void ShowStaticValue(int value)
    {
        StopRolling();

        if (!TryInitializeTextLists())
            return;

        SetRandomList(Mathf.Max(0, value));

        currentList.Clear();
        for (int li = 0; li < textLists.Count; li++)
        {
            int current = randomList.Count > li ? randomList[li] : 0;
            currentList.Add(current);

            List<Text> tlist = textLists[li];
            for (int i = 0; i < tlist.Count; i++)
            {
                RectTransform rectTransform = tlist[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                    rectTransform.anchoredPosition = new Vector2(0f, -H * VisibleCenterOffset + i * H);

                tlist[i].text = ((current + i) % 10).ToString();
            }
        }
    }

    public void PlayEnterRoll()
    {
        StartRoll(0, false);
    }

    public void PlayResultRoll()
    {
        StartRoll(randomInt, true);
    }

    private void StartRoll(int targetValue, bool isRewardRoll)
    {
        currentRollIsRewardRoll = isRewardRoll;
        currentTargetValue = targetValue;
        RollStarted?.Invoke(this, isRewardRoll);
        RefreshRoll(targetValue);
    }

    private void RefreshRoll(int targetValue)
    {
        StopRolling();

        if (!TryInitializeTextLists())
            return;

        currentList.Clear();
        for (int i = 0; i < textLists.Count; i++)
        {
            currentList.Add(0);
        }
        SetRandomList(targetValue);
        initCoroutine = StartCoroutine(OnInit());
    }

    private bool TryInitializeTextLists()
    {
        textLists.Clear();
        AddTextList(textList);
        AddTextList(textList1);
        AddTextList(textList2);
        AddTextList(textList3);

        if (textList == null || textList.Count == 0)
            return false;

        H = textList[0].GetComponent<RectTransform>().sizeDelta.y;
        return true;
    }

    private void StopRolling()
    {
        if (initCoroutine != null)
        {
            StopCoroutine(initCoroutine);
            initCoroutine = null;
        }

        for (int i = 0; i < activeTweens.Count; i++)
        {
            if (activeTweens[i] != null && activeTweens[i].active)
            {
                activeTweens[i].Kill();
            }
        }

        activeTweens.Clear();
    }

    private void AddTextList(List<Text> tlist)
    {
        if (tlist != null && tlist.Count > 0)
        {
            textLists.Add(tlist);
        }
    }

    public void SetRandomList(int num)
    {
        Debug.Log("Random " + num);
        randomList.Clear();

        do
        {
            int n = num % 10;
            randomList.Add((n - 2 + 10) % 10);
            num /= 10;
        } while (num > 0);

        for (int i = randomList.Count; i < textLists.Count; i++)
        {
            randomList.Add(8);
        }
    }

    private IEnumerator OnInit()
    {
        yield return null;
        initCoroutine = null;

        int counting = 0;
        int baseSpinSteps = GetBaseSpinSteps();
        int stopIntervalSteps = GetStopIntervalSteps();
        for (int li = 0; li < textLists.Count; li++)
        {
            List<Text> tlist = textLists[li];
            int targetOffset = randomList.Count > li ? randomList[li] : 8;
            int totalSteps = baseSpinSteps + targetOffset + li * stopIntervalSteps;
            int current = currentList[li];
            int index = li;
            counting++;

            Tween tween = CreateRollTween(tlist, current, totalSteps);

            tween.onComplete = () =>
            {
                current = (current + totalSteps) % 10;

                for (int i = 0; i < tlist.Count; i++)
                {
                    tlist[i].text = ((current + i) % 10).ToString();
                }

                currentList[index] = current;
                counting--;
                if (counting <= 0)
                {
                    OnRollComplete();
                }
            };

            activeTweens.Add(tween);
        }
    }

    private Tween CreateRollTween(List<Text> tlist, int current, int totalSteps)
    {
        int settleSteps = Mathf.Clamp(settleStepCount, 0, totalSteps);
        int fastSteps = totalSteps - settleSteps;
        Sequence sequence = DOTween.Sequence();

        if (fastSteps > 0)
        {
            sequence.Append(DOTween.To(() => 0f, x => UpdateRollingTexts(tlist, current, x), fastSteps, fastSteps * stepDuration)
                .SetEase(Ease.Linear));
        }

        if (settleSteps > 0)
        {
            sequence.Append(DOTween.To(() => (float)fastSteps, x => UpdateRollingTexts(tlist, current, x), totalSteps, settleSteps * stepDuration)
                .SetEase(Ease.OutCubic));
        }

        return sequence;
    }

    private void UpdateRollingTexts(List<Text> tlist, int current, float stepValue)
    {
        int tcurrent = (current + (int)Mathf.Floor(stepValue)) % 10;
        for (int i = 0; i < tlist.Count; i++)
        {
            tlist[i].GetComponent<RectTransform>().anchoredPosition =
                new Vector2(0, -H * line.Evaluate(stepValue) - H + i * H);
            tlist[i].text = ((tcurrent + i) % 10).ToString();
        }
    }

    private int GetBaseSpinSteps()
    {
        int spinCycles = Mathf.Max(1, Mathf.CeilToInt(baseRollCount / 10f));
        return spinCycles * 10;
    }

    private int GetStopIntervalSteps()
    {
        if (digitStaggerDelay <= 0f || stepDuration <= 0f)
        {
            return 0;
        }

        // Use whole digit cycles so each reel can stop later without changing the final number it lands on.
        int cyclesPerInterval = Mathf.Max(1, Mathf.CeilToInt(digitStaggerDelay / (stepDuration * 10f)));
        return cyclesPerInterval * 10;
    }

    private void OnRollComplete()
    {
        bool isRewardRoll = currentRollIsRewardRoll;
        int finalValue = currentTargetValue;

        currentRollIsRewardRoll = false;
        currentTargetValue = 0;

        RollCompleted?.Invoke(this, isRewardRoll, finalValue);
    }
}
