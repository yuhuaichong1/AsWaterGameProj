using cfg;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class GuideModule : BaseModule
{
    private int curStep;//当前引导步骤
    private GuideItem curGuideItems;//当前引导数据

    private bool ifTutorial;//是否处于引导状态

    protected override void OnLoad()
    {
        base.OnLoad();

        FacadeAdd();

        curGuideItems = new GuideItem();

        GetData();
    }

    #region Facade

    private void FacadeAdd()
    {
        FacadeGuide.NextStep += NextStep;
        FacadeGuide.GetCurGuideItems += GetCurGuideItems;
        FacadeGuide.GetCurStep += GetCurStep;
        FacadeGuide.NextStepHead += NextStepHead;
        FacadeGuide.GetIfTutorial += GetIfTutorial;
        FacadeGuide.SetIfTutorial += SetIfTutorial;
        FacadeGuide.PlayGuideByTargetType += PlayGuideByTargetType;
    }

    private void FacadeRemove()
    {
        FacadeGuide.NextStep -= NextStep;
        FacadeGuide.GetCurGuideItems -= GetCurGuideItems;
        FacadeGuide.GetCurStep -= GetCurStep;
        FacadeGuide.NextStepHead -= NextStepHead;
        FacadeGuide.GetIfTutorial -= GetIfTutorial;
        FacadeGuide.SetIfTutorial -= SetIfTutorial;
        FacadeGuide.PlayGuideByTargetType -= PlayGuideByTargetType;
    }

    #endregion

    /// <summary>
    /// 获取引导数据
    /// </summary>
    private void GetData()
    {
        ifTutorial = SPlayerPrefs.GetBool(PlayerPrefDefines.ifTutorial, true);    
        curStep = SPlayerPrefs.GetInt(PlayerPrefDefines.curStep);
        if (curStep == 0) curStep = GameDefines.firstGuideId;
        else
        {
            var backStep = ConfigModule.Instance.Tables.TBGuides.Get(curStep).BackStep;
            curStep = backStep != 0 ? backStep : curStep;
        }

        SetCurGuideItems(curStep);
    }

    /// <summary>
    /// 设置当前引导数据
    /// </summary>
    /// <param name="step">引导步骤</param>
    private void SetCurGuideItems(int step)
    {
        curStep = step;
        ConfGuides guideData = ConfigModule.Instance.Tables.TBGuides.Get(step);
        curGuideItems.step = step;
        curGuideItems.nextStep = guideData.NextStep;
        curGuideItems.note = guideData.Notes;
        curGuideItems.backStep = guideData.BackStep;
        curGuideItems.ifBackPlay = guideData.IfBackPlay;
        curGuideItems.ifNextStep = guideData.IfNextStep;
        curGuideItems.ifNextPlay = guideData.IfNextPlay;
        curGuideItems.autohiddenTime = guideData.AutohiddenTime;
        curGuideItems.diglogContent = GetLanguageContent(guideData.DiglogContentId);
        curGuideItems.diglogPos = GetUIRectTrans(guideData.DiglogPos);
        curGuideItems.handPos = GetUIRectTrans(guideData.HandPos);
        curGuideItems.onMaskObjs = GetUIRectTrans(guideData.OnMaskObjs);
        curGuideItems.ifMask = guideData.IfMask;
        curGuideItems.transparentPos = GetUIRectTrans(guideData.TransparentPos);
        curGuideItems.clickPos = GetClickRectTrans(guideData.ClickPos);
        curGuideItems.extraStart = guideData.ExtraStart;
        curGuideItems.extraEnd = guideData.ExtraEnd;
    }

    /// <summary>
    /// 获取当前引导数据
    /// </summary>
    /// <returns>当前引导数据</returns>
    private GuideItem GetCurGuideItems()
    {
        return curGuideItems;
    }

    /// <summary>
    /// 进入下一步引导（依据条件判断是否进入）
    /// </summary>
    private void NextStep()
    {
        if (curGuideItems.ifNextStep && !CheckGuideEnd())
        {
            FacadeGuide.RestorePreOnMaskObjs?.Invoke();

            SetExtraEnd(curGuideItems.extraEnd);

            bool orginBool = curGuideItems.ifNextPlay;
            curStep = curGuideItems.nextStep;
            SetCurGuideItems(curStep);
            if (orginBool)
            {
                FacadeGuide.PlayGuide();
            }
            else
            {
                FacadeGuide.CloseGuide();
            }
        }
        else
        {
            FacadeGuide.CloseGuide();
        }

        SPlayerPrefs.SetInt(PlayerPrefDefines.curStep, curStep);
        SPlayerPrefs.Save();
    }

    /// <summary>
    /// 手动（强制）进入下一步骤
    /// </summary>
    private void NextStepHead()
    {
        curStep = curGuideItems.nextStep;
        CheckGuideEnd();
        SetCurGuideItems(curStep);
        SPlayerPrefs.SetInt(PlayerPrefDefines.curStep, curStep);
        SPlayerPrefs.Save();
    }

    /// <summary>
    /// 获取语言集合
    /// </summary>
    /// <param name="languageIds">语言id</param>
    /// <returns>目标语言文本集合</returns>
    private List<string> GetLanguageContent(List<int> languageIds)
    {
        if (languageIds == null || languageIds.Count == 0) return null;

        List<string> temp = new List<string>();
        foreach (int languageId in languageIds)
        {
            temp.Add(FacadeLanguage.GetText(languageId.ToString()));
        }

        return temp;
    }

    /// <summary>
    /// 获取UI路径
    /// </summary>
    /// <param name="pathData">路径的str数据</param>
    /// <returns>目标UI</returns>
    private List<string> GetUIRectTrans(List<string> pathData)
    {
        if (pathData == null || pathData.Count == 0) return null;

        List<string> temp = new List<string>();
        foreach (string item in pathData)
        {
            string[] diglongPosStr = item.Split(',');
            string path = UIManager.Instance.GetUIPath(diglongPosStr);
            temp.Add(path);
        }
        return temp;
    }

    /// <summary>
    /// 单独处理可点击部分的位置
    /// </summary>
    /// <param name="pathData">路径（可能是其他类型）</param>
    /// <param name="item">当前引导项目</param>
    /// <returns>目标路径</returns>
    private List<string> GetClickRectTrans(List<string> pathData)
    {
        List<string> temp = new List<string>();
        for (int i = 0; i < pathData.Count; i++)
        {
            switch (pathData[i])
            {
                case "handPos":
                    temp.Add(curGuideItems.handPos[i]);
                    break;
                case "transparentPos":
                    temp.Add(curGuideItems.transparentPos[i]);
                    break;
                default:
                    temp.Add(GetUIRectTrans(new List<string>() { pathData[i] })[0]);
                    break;
            }
        }

        return temp;
    }

    /// <summary>
    /// 得到当前引导步骤
    /// </summary>
    /// <returns>当前引导步骤</returns>
    private int GetCurStep()
    {
        return curStep;
    }

    /// <summary>
    /// 获取当前是否处于引导状态
    /// </summary>
    /// <returns>当前是否处于引导状态</returns>
    private bool GetIfTutorial()
    {
        return ifTutorial;
    }

    /// <summary>
    /// 设置当前是否处于引导状态
    /// </summary>
    /// <param name="b">当前是否处于引导状态</param>
    private void SetIfTutorial(bool b)
    {
        ifTutorial = b;
        SPlayerPrefs.SetBool(PlayerPrefDefines.ifTutorial, ifTutorial);
        SPlayerPrefs.Save();
    }

    /// <summary>
    /// 检测引导是否结束
    /// </summary>
    /// <returns>引导是否结束</returns>
    private bool CheckGuideEnd()
    {
        var nextStep = ConfigModule.Instance.Tables.TBGuides.Get(curStep)?.NextStep ?? 0;
        var isEnd = nextStep == 0;
        if (isEnd)
        {
            D.Error("Guide End");
            ifTutorial = false;
            SetIfTutorial(ifTutorial);
            SetExtraEnd(curGuideItems.extraEnd);
        }
        return isEnd;
    }

    /// <summary>
    /// 激活引导结束时的回调
    /// </summary>
    /// <param name="extraData">回调数据类型</param>
    private void SetExtraEnd(Dictionary<string, string> extraData)
    {
        foreach (KeyValuePair<string, string> kvp in extraData)
        {
            switch (kvp.Key)
            {
                case "handC":
                    FacadeGuide.SetHandCorrection(bool.Parse(kvp.Value) ? -50 : 0);
                    break;
                case "iac":
                    FacadeWithdraw.SetIfAfterCreate(true);
                    break;
            }
        }
    }


    #region 拓展

    /// <summary>
    /// 根据当前的兑现目标类型执行对应的引导
    /// </summary>
    private void PlayGuideByTargetType()
    {
        FacadeWithdraw.ActionByCurWTarget((value) =>
        {
            if(value == 1)
            {
                if(!GameDefines.ifIAA)
                {
                    UIManager.Instance.OpenAsync<UIWithdrawTarget>(EUIType.EUIWithdrawTarget, UIOpenType.None, null, UIWTOpenType.FirstTarget, (Action)PlayGuideByTargetType2);
                }
                else
                {
                    FacadeGamePlay.CreateLevel();
                    ShowIAAGuide();
                }
            }
            else if (value == 2)
            {
                if (!GameDefines.ifIAA)
                {
                    UIManager.Instance.OpenAsync<UIWithdrawTarget>(EUIType.EUIWithdrawTarget, UIOpenType.None, null, UIWTOpenType.FinishTarget1, (Action)BeforeTarget_Level2);
                }
                else
                {
                    FacadeGamePlay.CreateLevel();
                }

            }
            else if(value == 3)
            {
                if (!GameDefines.ifIAA)
                {
                    UIManager.Instance.OpenAsync<UIWithdrawTarget>(EUIType.EUIWithdrawTarget, UIOpenType.None, null, UIWTOpenType.FinishTarget1, (Action)BeforeTarget_Level3);
                }
                else
                {
                    FacadeGamePlay.CreateLevel();
                }
            }
        }, (value) =>
        {
            if (!GameDefines.ifIAA)
            {
                UIManager.Instance.OpenAsync<UIWithdrawGoal>(EUIType.EUIWithdrawGoal, UIOpenType.None, (BaseUI) =>
                {
                    curStep = 10008;
                    PlayGuideByTargetType3();
                }, true, false);
            }
            else
            {
                FacadeGamePlay.CreateLevel();
            }
        }, (value) => 
        {
            if (!GameDefines.ifIAA)
            {
                UIManager.Instance.OpenAsync<UIWithdrawGoal>(EUIType.EUIWithdrawGoal, UIOpenType.None, (BaseUI) =>
                {
                    curStep = 10009;
                    PlayGuideByTargetType3();
                }, true, false);
            }
            else
            {
                FacadeGamePlay.CreateLevel();
            }
        });
    }

    private void ShowIAAGuide()
    {
        STimerManager.Instance.CreateSDelay(0.2f, () =>
        {
            curStep = 10001;
            SetCurGuideItems(curStep);
            if (FacadeGuide.PlayGuide == null)
                ShowIAAGuide();
            else
                FacadeGuide.PlayGuide();
        });
    }

    private void BeforeTarget_Level2()
    {
        UIManager.Instance.OpenAsync<UIWithdrawGoal>(EUIType.EUIWithdrawGoal, UIOpenType.None, (BaseUI) =>
        {
            curStep = 10006;
            PlayGuideByTargetType3();

        }, true, false);
    }

    private void BeforeTarget_Level3()
    {
        UIManager.Instance.OpenAsync<UIWithdrawGoal>(EUIType.EUIWithdrawGoal, UIOpenType.None, (BaseUI) =>
        {
            curStep = 10007;
            PlayGuideByTargetType3();

        }, true, false);
    }

    private void PlayGuideByTargetType2()
    {
        FacadeGamePlay.CreateLevel();
        STimerManager.Instance.CreateSDelay(0.2f, () =>
        {
            curStep = 10001;
            SetCurGuideItems(curStep);
            FacadeGuide.PlayGuide();
        });
    }

    private void PlayGuideByTargetType3()
    {
        SetCurGuideItems(curStep);
        if (FacadeGuide.PlayGuide == null)
        {
            STimerManager.Instance.CreateSDelay(0.2f, () =>
            {
                PlayGuideByTargetType3();
            });
        }
        else
        {
            STimerManager.Instance.CreateSDelay(GameDefines.ShowAnimTime, () =>
            {
                FacadeGuide.PlayGuide();
            });
        }
    }

    #endregion

    protected override void OnDispose()
    {
        FacadeRemove();

        curGuideItems = null;
    }
}
