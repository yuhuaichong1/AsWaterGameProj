
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UILuckyReward : BaseUI
    {
        private float curCompletedMoney;
        private float curOnlyMoney;
        protected override void OnAwake()
        {
            mMoneyRewardBigIcon.gameObject.SetActive(!GameDefines.ifIAA);
            mIAAMoneyRewardBigIcon.gameObject.SetActive(GameDefines.ifIAA);
            mMoneyIcon.gameObject.SetActive(!GameDefines.ifIAA);
            mIAAMoneyIcon.gameObject.SetActive(GameDefines.ifIAA);
        }
        protected override void OnEnable()
        {
            curCompletedMoney = FacadeWithdraw.GetLuckyReward();
            curOnlyMoney = curCompletedMoney / 10;
            mMoneyText.text = $"+{FacadePayType.RegionalChange(curCompletedMoney)}";
            mOnlyText.text = string.Format(FacadeLanguage.GetText("10019"), FacadePayType.RegionalChange(curCompletedMoney / 10));

            ShowAnim(mPlane);
        }

        private void OnAdBtnClickHandle()
        {
            FacadeAd.PlayROIAdByWeight(EAdSource.LuckyReward, (count) => { GetReward(); }, (errMsg) => { GetOnlyReward(); }, ()=> { GetOnlyReward(); }, GameDefines.WeightAdRange, GameDefines.AdWeight);
        }
        private void OnOnlyBtnClickHandle()
        {
            FacadeAd.AdRefuse(EAdSource.Refuse_LuckyReward, (count)=> { GetReward(); }, (errMsg)=> 
            {
                TDAnalyticsManager.Instance.OnlyAdFailedCount();
                GetOnlyReward();
            }, () => 
            {
                TDAnalyticsManager.Instance.OnlyAdFailedCount();
                GetOnlyReward();
            });
        }

        private void GetReward()
        {
            FacadePlayer.AddMoney(curCompletedMoney);
            FacadeGamePlay.ReStartLRTimer();

            HideAnim(mPlane, () =>
            {
                //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                //{
                //new ERewardItemStruct()
                //{
                //    Type = ERewardType.Money,
                //    Count = curCompletedMoney,
                //}
                //}, null);
                FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                {
                    Type = ERewardType.Money,
                    Count = curCompletedMoney,
                }, null);

                UIManager.Instance.CloseUI(EUIType.EUILuckyReward);
            });
        }

        private void GetOnlyReward()
        {
            FacadePlayer.AddMoney(curOnlyMoney);
            FacadeGamePlay.ReStartLRTimer();
            HideAnim(mPlane, () => 
            {
                FacadeEffect.PlayFlyMoney(mOnlyBtn.transform, GameDefines.FlyMoney_FlyMoneyCount, curOnlyMoney, () => { FacadeGamePlay.SetCurMoneyShow(); });
                UIManager.Instance.CloseUI(EUIType.EUILuckyReward);
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