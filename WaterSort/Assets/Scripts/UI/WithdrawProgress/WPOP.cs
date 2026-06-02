using System;
using System.Collections.Generic;
using UnityEngine;

public class WPOP : MonoBehaviour
{
    public List<WPOPItem> WPOrderProgress;

    private int curId;
    private STimer timer;

    public void PlayAnim(Action finishAction, bool ifFail = false, int failStep = 0)
    {
        if (failStep > WPOrderProgress.Count - 1)
            ifFail = false;

        curId = 0;
        int progressGold = ifFail ? failStep - 1 : WPOrderProgress.Count;

        for (int i = 0; i < WPOrderProgress.Count; i++)
        {
            WPOrderProgress[i].FinishedIcon.SetActive(false);
            WPOrderProgress[i].LoadingIcon.gameObject.SetActive(true);
            WPOrderProgress[i].Obj.gameObject.SetActive(false);
        }

        WPOrderProgress[0].Obj.gameObject.SetActive(true);

        if (timer == null)
            timer = STimerManager.Instance.CreateSTimer(1, progressGold - 1, true, false, () => 
            {
                WPOrderProgress[curId].FinishedIcon.SetActive(true);
                WPOrderProgress[curId].LoadingIcon.gameObject.SetActive(false);
                curId++;
                if (curId < WPOrderProgress.Count)
                {
                    WPOrderProgress[curId].Obj.gameObject.SetActive(true);
                }
                if (curId == progressGold)
                {
                    finishAction?.Invoke();
                }
            });
        else
            timer.ReStart();
    }
}