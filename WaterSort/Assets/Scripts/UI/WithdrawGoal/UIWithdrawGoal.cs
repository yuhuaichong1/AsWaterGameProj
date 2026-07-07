
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawGoal : BaseUI
    {
        bool ifFromGuide;
        bool ifFromLuckyUser;

        protected override void OnAwake()
        {
            mNoticeScrollView.Play();
        }

        protected override void OnSetParam(params object[] args)
        {
            if (args.Length >= 1)
                ifFromGuide = (bool)args[0];
            if (args.Length >= 2)
                ifFromLuckyUser = (bool)args[1];
        }


        protected override void OnEnable()
        {
            mBlanceMoney.text = FacadePayType.RegionalChange(FacadePlayer.GetMoney());

            FacadeWithdraw.ActionByCurWTarget((value) => 
            {
                mGoalTitle.text = string.Format(FacadeLanguage.GetText("10066"), FacadeWithdraw.GetWithdrawalRecordItems().Count + (ifFromGuide ? 1 : 0));
            }, (value) => 
            {
                mGoalTitle.text = string.Format(FacadeLanguage.GetText("10066"), 3);
            }, (value) => 
            {
                int count = FacadeWithdraw.GetWithdrawalRecordItems().Count;
                if(count == 3)
                    mGoalTitle.text = string.Format(FacadeLanguage.GetText("10066"), 3);
                else
                    mGoalTitle.text = string.Format(FacadeLanguage.GetText("10066"), FacadeWithdraw.GetWithdrawalRecordItems().Count + (ifFromGuide ? 1 : 0));
            });
            
            

            SetSliderValue();
            SetLevelStatus();
            SetGoalText();
            if (ifFromGuide)
            {
                ifFromGuide = false;
            }

            ShowAnim(mPlane);
        }

        /// <summary>
        /// 设置进度条显示
        /// </summary>
        private void SetSliderValue()
        {
            FacadeWithdraw.ActionByCurWTarget((level) =>
            {
                if (level == 1)
                {
                    mGoalSlider.value = 0;
                    mGoldSliderText.text = "0/1";
                }
                else if (level == 2)
                {
                    if (ifFromGuide)
                    {
                        mGoalSlider.value = 1;
                        mGoldSliderText.text = "1/1";
                    }
                    else
                    {
                        mGoalSlider.value = 0.5f;
                        mGoldSliderText.text = "1/2";
                    }
                }
                else if (level < GameDefines.miniLevel_Start)
                {
                    if (ifFromGuide)
                    {
                        mGoalSlider.value = 1;
                        mGoldSliderText.text = "2/2";
                    }
                    else
                    {
                        mGoalSlider.value = level * 1f / (GameDefines.miniLevel_Start - 1);
                        mGoldSliderText.text = $"{level}/{(GameDefines.miniLevel_Start - 1)}";
                    }
                }
                else
                {
                    mGoalSlider.value = level * 1f / GameDefines.miniLevel_End;
                    mGoldSliderText.text = $"{level + 2 - GameDefines.miniLevel_Start}/{GameDefines.miniLevel_End - GameDefines.miniLevel_Start + 2}";
                }
            }, (money) =>
            {
                if (ifFromGuide)
                {
                    mGoalSlider.value = 1;
                    int temp = GameDefines.miniLevel_End - GameDefines.miniLevel_Start + 2;
                    mGoldSliderText.text = $"{temp}/{temp}";
                }
                else
                {
                    double wTargetMoney = FacadeWithdraw.GetWTarget();
                    mGoldSliderText.text = $"{(int)(FacadePlayer.GetMoney() * FacadePayType.GetExchangeRate())}/{(int)(wTargetMoney * FacadePayType.GetExchangeRate())}";
                    mGoalSlider.value = (float)(FacadePlayer.GetMoney() / wTargetMoney);
                }
            }, (day) =>
            {
                if (ifFromGuide)
                {
                    double wTargetMoney = FacadeWithdraw.GetWTarget();
                    mGoldSliderText.text = $"{(int)(FacadePlayer.GetMoney() * FacadePayType.GetExchangeRate())}/{(int)(wTargetMoney * FacadePayType.GetExchangeRate())}";
                    mGoalSlider.value = (float)(FacadePlayer.GetMoney() / wTargetMoney);
                }
                else
                {
                    mGoldSliderText.text = $"{day}/{GameDefines.CheckInDay}";
                    mGoalSlider.value = day * 1f / GameDefines.CheckInDay;
                }
            });
        }

        /// <summary>
        /// 设置关卡状态
        /// </summary>
        private void SetLevelStatus()
        {
            mLevelStatsText.text = string.Format(FacadeLanguage.GetText("10068"), DateTime.Now.ToString("dd/MM/yyyy"));
            //mTCValue.text = "";
            //mAAValue.text = "";
            mAWValue.text = FacadePayType.RegionalChange(4000);
        }

        /// <summary>
        /// 设置目标文本
        /// </summary>
        private void SetGoalText()
        {
            FacadeWithdraw.ActionByCurWTarget((level) =>
            {
                if (level == 1 || level == 2)
                {
                    if (ifFromGuide)
                    {
                        mGoalText.text = string.Format(FacadeLanguage.GetText("10005"), 1);
                    }
                    else
                    {
                        mGoalText.text = string.Format(FacadeLanguage.GetText("10005"), level);
                    }
                }
                else
                {
                    if (ifFromGuide)
                    {
                        mGoalText.text = string.Format(FacadeLanguage.GetText("10005"), 2);
                    }
                    else
                    {
                        mGoalText.text = string.Format(FacadeLanguage.GetText("10005"), GameDefines.miniLevel_Start - 1);
                    }
                }
            }, (money) =>
            {
                if (ifFromGuide)
                {
                    mGoalText.text = string.Format(FacadeLanguage.GetText("10005"), GameDefines.miniLevel_Start - 1);
                }
                else
                {
                    mGoalText.text = string.Format(FacadeLanguage.GetText("10098"), FacadePayType.RegionalChange(FacadeWithdraw.GetWTarget()));
                }
            }, (day) =>
            {
                if (ifFromGuide)
                {
                    mGoalText.text = string.Format(FacadeLanguage.GetText("10098"), FacadePayType.RegionalChange(FacadeWithdraw.GetWTarget()));
                }
                else
                {
                    mGoalText.text = string.Format(FacadeLanguage.GetText("10099"), $"{FacadeWithdraw.GetCurCheckInDay()}/{GameDefines.CheckInDay}");
                }
            });
        }

        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal);
                if (ifFromLuckyUser)
                {
                    ifFromLuckyUser = false;
                    //FacadeGamePlay.StartLevel();
                    UIManager.Instance.OpenAsync<UIDateShow>(EUIType.EUIDateShow);
                }
            });

        }

        private void OnWithdrawBtnClickHandle()
        {
            FacadeWithdraw.ActionByCurWTarget((level) =>
            {
                if (FacadeWithdraw.GetCanWithdraw())
                {
                    FacadeWithdraw.SetCanWithdraw(false);
                    WithdrawalRecordItem UIProgressTarget = FacadeWithdraw.CreateOrder(FacadePlayer.GetLevel() - 1, (float)FacadePlayer.GetMoney());
                    FacadePlayer.SetMoney(0);
                    FacadeGamePlay.SetCurMoneyShow();
                    HideAnim(mPlane, () =>
                    {
                        UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal);
                        if (FacadeWithdraw.GetPayType() == EPayType.None)
                        {
                            UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo, UIOpenType.None, null, UIProgressTarget);
                        }
                        else
                        {
                            UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, UIProgressTarget);
                        }
                    });
                }
                else
                {
                    UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10102"));
                }
            }, (money) =>
            {
                if (FacadeWithdraw.GetCanWithdraw())
                {
                    FacadeWithdraw.SetCanWithdraw(false);
                    WithdrawalRecordItem UIProgressTarget;
                    if (FacadeWithdraw.GetWithdrawalRecordItems().Count == 3)
                    {
                        UIProgressTarget = FacadeWithdraw.GetWithdrawalRecordItemById(2);
                        UIProgressTarget.WRMoney = FacadePlayer.GetMoney();
                    }
                    else
                    {
                        UIProgressTarget = FacadeWithdraw.CreateOrder(FacadePlayer.GetLevel() - 1, (float)FacadePlayer.GetMoney());
                    }

                    HideAnim(mPlane, () =>
                    {
                        UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal);
                        if (FacadeWithdraw.GetPayType() == EPayType.None)
                        {
                            UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo, UIOpenType.None, null, UIProgressTarget);
                        }
                        else
                        {
                            UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, UIProgressTarget);
                        }
                    });
                }
                else
                {
                    HideAnim(mPlane, () =>
                    {
                        WithdrawalRecordItem UIProgressTarget = FacadeWithdraw.GetWithdrawalRecordItemById(2);

                        UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal);
                        if (FacadeWithdraw.GetPayType() == EPayType.None)
                        {
                            UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo, UIOpenType.None, null, UIProgressTarget);
                        }
                        else
                        {
                            UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, UIProgressTarget);
                        }
                    });
                }
            }, (day) =>
            {
                HideAnim(mPlane, () =>
                {
                    WithdrawalRecordItem UIProgressTarget = FacadeWithdraw.GetWithdrawalRecordItemById(2);
                    UIProgressTarget.TargetType = WithdrawTarget.CheckIn;

                    UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal);
                    if (FacadeWithdraw.GetPayType() == EPayType.None)
                    {
                        UIManager.Instance.OpenAsync<UIWithdrawEnterInfo>(EUIType.EUIWithdrawEnterInfo, UIOpenType.None, null, UIProgressTarget);
                    }
                    else
                    {
                        UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, UIProgressTarget);
                    }
                });
            });
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}