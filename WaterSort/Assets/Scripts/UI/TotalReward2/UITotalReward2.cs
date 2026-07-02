
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UITotalReward2 : BaseUI
    {
        protected override void OnAwake()
        {
            mMoneyText.text = FacadePayType.RegionalChange(GameDefines.totalRewardMoney);
        }
        protected override void OnEnable()
        {
            mContent.text = string.Format(FacadeLanguage.GetText("10156"), FacadeWithdraw.GetRemainTRDay());

            ShowAnim(mPlane);
        }
        	    private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUITotalReward2);
            });
            
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}