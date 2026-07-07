
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawLuckyPlayer : BaseUI
    {
        private double luckyMoney;

        protected override void OnAwake()
        {
            
        }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);

            luckyMoney = FacadeWithdraw.GetRemainTarget() - GameDefines.DiffVal;
            mLPMoney.text = FacadePayType.RegionalChange(luckyMoney);
            mDesc.text = string.Format(FacadeLanguage.GetText("10074"), GameDefines.LP_PackCount, GameDefines.LP_PlayerNo, mLPMoney.text);
        }

        private void OnExitBtnClickHandle()
        {
            FacadeGuide.SetIfTutorial(false);

            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawLuckyPlayer);
                //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                //{
                //    new ERewardItemStruct
                //    {
                //        Type = ERewardType.Money,
                //        Count = luckyMoney,
                //    }
                //}, null);
                FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                {
                    Type = ERewardType.Money,
                    Count = luckyMoney
                }, null);

                FacadeGamePlay.StartLevel();
                
            });
        }

        private void OnConfirmBtnClickHandle()
        {
            //FacadeGuide.SetIfTutorial(false);

            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawLuckyPlayer);
                //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                //{
                //    new ERewardItemStruct
                //    {
                //        Type = ERewardType.Money,
                //        Count = luckyMoney,
                //    }
                //}, null);
                FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                {
                    Type = ERewardType.Money,
                    Count = luckyMoney
                }, null);

                //FacadeGamePlay.StartLevel();
                UIManager.Instance.OpenAsync<UIWithdrawGoal>(EUIType.EUIWithdrawGoal, UIOpenType.None, null, false, true);
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