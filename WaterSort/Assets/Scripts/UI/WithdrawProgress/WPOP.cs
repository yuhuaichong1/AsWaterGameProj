using System;
using System.Collections.Generic;
using UnityEngine;

public class WPOP : MonoBehaviour
{
    public List<WPOPItem> WPOrderProgress;

    private int curId;
    private STimer timer;

    public void PlayAnim(Action finishAction, int failStep = (-1))
    {
        if (failStep > WPOrderProgress.Count || failStep == -1)
            failStep = WPOrderProgress.Count;

        curId = 0;
        int progressGold = WPOrderProgress.Count;

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
                if(curId < failStep)
                {
                    WPOrderProgress[curId].FinishedIcon.SetActive(true);
                    WPOrderProgress[curId].LoadingIcon.gameObject.SetActive(false);
                }

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

    public void SetOrderTime(string time)
    {
        for(int i = 0;i < WPOrderProgress.Count;i++)
        {
            WPOrderProgress[i].Content.text = time;
        }
    }
}