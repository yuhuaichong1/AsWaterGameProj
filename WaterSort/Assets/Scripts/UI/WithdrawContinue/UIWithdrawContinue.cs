
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawContinue : BaseUI
    {
        string highValueText;

        protected override void OnAwake()
        {
            string v1 = FacadePayType.RegionalChange(GameDefines.HighValue.x).Split('.')[0];
            string v2 = FacadePayType.RegionalChange(GameDefines.HighValue.y).Split('.')[0];
            highValueText = $"{v1}-{v2}";

            mWithText.text = string.Format(FacadeLanguage.GetText("10109"), FacadeWithdraw.GetWithdrawHighValueStr());
        }
        protected override void OnEnable()
        {
            int curLevel = FacadePlayer.GetLevel();
            List<WithdrawalRecordItem> WHRecord = FacadeWithdraw.GetWithdrawalRecordItems();

            bool c1 = curLevel > 1;
            mChat_1_GrayBg.gameObject.SetActive(c1);

            mChat_1_Money.text = FacadePayType.RegionalChange(c1 ? WHRecord[0].WRMoney : 0.03);
            mChat_1_Statu.text = c1 ? string.Format(FacadeLanguage.GetText("10111"), 1) : FacadeLanguage.GetText("10110");

            bool c2 = curLevel > 2;
            mChat_2_GrayBg.gameObject.SetActive(c2);
            mChat_2_Money.text = FacadePayType.RegionalChange(c2 ? WHRecord[1].WRMoney : 0.04);
            mChat_2_Statu.text = c2 ? string.Format(FacadeLanguage.GetText("10111"), 2) : FacadeLanguage.GetText("10110");

            bool c3 = curLevel > GameDefines.miniLevel_Start;
            mChat_3_GrayBg.gameObject.SetActive(c3);
            mChat_3_Money.text = c3 ? FacadePayType.RegionalChange(WHRecord[2].WRMoney) : highValueText;
            mChat_3_Statu.text = c3 ? string.Format(FacadeLanguage.GetText("10111"), GameDefines.miniLevel_Start - 1) : FacadeLanguage.GetText("10110");

            ShowAnim(mPlane);
        }
        	    private void OnContiuneBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                FacadeGamePlay.StartLevel();
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawContinue);
            });
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}