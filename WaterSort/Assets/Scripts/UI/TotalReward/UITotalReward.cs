
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UITotalReward : BaseUI
    {
        protected override void OnAwake()
        {
            mMoneyText.text = FacadePayType.RegionalChange(GameDefines.totalRewardMoney);
            mContent.text = string.Format(FacadeLanguage.GetText("10154"), GameDefines.totalRewardLevel);
        }
        protected override void OnEnable()
        {
            int curLevel = FacadePlayer.GetLevel();
            if(curLevel > GameDefines.totalRewardLevel)
                curLevel = GameDefines.totalRewardLevel;

            mLevelSlider.value = (float)curLevel / GameDefines.totalRewardLevel;
            mSliderText.text = $"{curLevel} / {GameDefines.totalRewardLevel}";

            //mUnClaimBtn.gameObject.SetActive(curLevel != GameDefines.totalRewardLevel);
            mUnClaimBtn.gameObject.SetActive(false);

            ShowAnim(mPlane);
        }
        	    private void OnClaimBtnClickHandle()        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUITotalReward);
                FacadeWithdraw.SetTrStatus(TrStatus.WaitResults);
                FacadeWithdraw.RefushWaitDay(true);
                UIManager.Instance.OpenAsync<UITotalReward2>(EUIType.EUITotalReward2);
            });        }	    
        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUITotalReward);
            });
        }
        
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}