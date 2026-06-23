using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UILevelCompleted : BaseUI
    {
        private int curCompletedLevel;//当前完成的关卡
        private float curCompletedMoney;//当前完成的关卡的通关奖励
        private float curOnlyMoney;//通关奖励÷10

        protected override void OnAwake()
        {
            mMoneyIcon.gameObject.SetActive(!GameDefines.ifIAA);
            mIAAMoneyIcon.gameObject.SetActive(GameDefines.ifIAA);
            mMoneyRewardBigIcon.gameObject.SetActive(!GameDefines.ifIAA);
            mIAAMoneyRewardBigIcon.gameObject.SetActive(GameDefines.ifIAA);
        }

        protected override void OnSetParam(params object[] args)
        {
            curCompletedLevel = (int)args[0];
        }

        protected override void OnEnable()
        {
            InitShow();
            FacadeAudio.PlayEffect(EAudioType.EWin);

            if(GameDefines.ifIAA)
            {
                mAdBtn.gameObject.SetActive(true);
                mOnlyText.gameObject.SetActive(true);
                mWithdrawBtn.gameObject.SetActive(false);
            }
            else
            {
                bool ifWLv = ifWlevel();
                mAdBtn.gameObject.SetActive(!ifWLv);
                mOnlyText.gameObject.SetActive(!ifWLv);
                mWithdrawBtn.gameObject.SetActive(ifWLv);
                if (ifWLv)
                    mMoneyText.text = FacadePayType.RegionalChange(FacadePlayer.GetMoney());
            }

            ShowAnim(mPlane);
        }

        private void InitShow()
        {
            curCompletedMoney = FacadeWithdraw.GetLevelComplateReward();
            curOnlyMoney = curCompletedMoney / 10;
            mMoneyText.text = $"+{FacadePayType.RegionalChange(curCompletedMoney)}";
            mOnlyText.text = string.Format(FacadeLanguage.GetText("10019"), FacadePayType.RegionalChange(curOnlyMoney));
        }
        
	    private void OnAdBtnClickHandle()
        {
            FacadeAd.PlayROIAdByWeight(EAdSource.LevelCompleted, (count) => { GetReward(); }, (errMsg) => { GetOnlyReward(); }, ()=> { GetOnlyReward(); }, GameDefines.WeightAdRange, GameDefines.AdWeight);
        }

        private void OnWithdrawBtnClickHandle()
        {
            GoNextLevel();
        }

        private void OnOnlyBtnClickHandle()
        {
            FacadeAd.AdRefuse(EAdSource.Refuse_LevelComplate, (count) => { GetReward(); }, (errMsg) => { GetOnlyReward(); }, () => { GetOnlyReward(); });
        }

        private void GoNextLevel()
        {
            UIManager.Instance.CloseUI(EUIType.EUILevelCompleted);
            FacadeGamePlay.StartLevel();
        }

        private void GetReward()
        {
            FacadePlayer.AddMoney(curCompletedMoney);
            FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
            {
                new ERewardItemStruct
                {
                    Type = ERewardType.Money,
                    Count = curCompletedMoney,
                }
            }, null);

            FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct 
            {
                Type = ERewardType.Money,
                Count = curCompletedMoney,
            }, null);

            GoNextLevel();
        }

        private void GetOnlyReward()
        {
            FacadePlayer.AddMoney(curOnlyMoney);
            FacadeEffect.PlayFlyMoney(mOnlyBtn.transform, GameDefines.FlyMoney_FlyMoneyCount, curOnlyMoney, () => { FacadeGamePlay.SetCurMoneyShow(); });
            GoNextLevel();
        }

        private bool ifWlevel()
        {
            int[] Wlevels = GameDefines.WithdrawalLevels;
            for (int i = 0; i < Wlevels.Length; i++) 
            { 
                if (curCompletedLevel == Wlevels[i])
                {
                    return true;
                }
            }
            return false;
        }

        protected override void OnDisable() 
        {
        
        }
        protected override void OnDispose()
        {
            
        }
    }
}