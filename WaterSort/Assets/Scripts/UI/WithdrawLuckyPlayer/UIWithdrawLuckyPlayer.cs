
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawLuckyPlayer : BaseUI
    {
        private float luckyMoney;

        protected override void OnAwake()
        {
            
        }
        protected override void OnEnable()
        {
            luckyMoney = FacadeWithdraw.GetRemainTarget() - GameDefines.DiffVal;
            mLPMoney.text = FacadePayType.RegionalChange(luckyMoney);
            mDesc.text = string.Format(FacadeLanguage.GetText("10074"), GameDefines.LP_PackCount, GameDefines.LP_PlayerNo, mLPMoney.text);
        }

        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawLuckyPlayer);
                FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                {
                    new ERewardItemStruct
                    {
                        Type = ERewardType.Money,
                        Count = luckyMoney,
                    }
                }, null);
            });
        }

        private void OnConfirmBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawLuckyPlayer);
                FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                {
                    new ERewardItemStruct
                    {
                        Type = ERewardType.Money,
                        Count = luckyMoney,
                    }
                }, null);
            });
        }

        protected override void OnDisable()
        {
            
        }

        protected override void OnDispose()
        {
            
        }
    }
}