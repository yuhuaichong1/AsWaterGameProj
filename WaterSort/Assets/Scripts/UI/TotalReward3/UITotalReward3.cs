
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UITotalReward3 : BaseUI
    {
        protected override void OnAwake()
        {
            mCPText.text = FacadePayType.RegionalChange(GameDefines.totalRewardMoney2);
        }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);
            FacadePlayer.AddMoney(GameDefines.totalRewardMoney2);
            FacadeWithdraw.SetTrStatus(TrStatus.WaitNext);
            FacadeWithdraw.RefushWaitDay(true);
        }
        	    private void OnClaimBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct()
                {
                    Type = ERewardType.Money,
                    Count = GameDefines.totalRewardMoney2,
                }, () => 
                {
                    FacadeGamePlay.SetCurMoneyShow();
                });
                UIManager.Instance.CloseUI(EUIType.EUITotalReward3);
            });
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}