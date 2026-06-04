using System;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawProgress : BaseUI
    {
        private WithdrawalRecordItem item;

        protected override void OnAwake()
        {

        }

        protected override void OnSetParam(params object[] args)
        {
            item = (WithdrawalRecordItem)args[0];
            mCurMoneyText.text = FacadePayType.RegionalChange(item.WRMoney);
        }

        protected override void OnEnable() 
        {
            mExitBtn.gameObject.SetActive(false);
            mConfirmBtn.gameObject.SetActive(false);
            mProgress1.gameObject.SetActive(false);
            mProgress2.gameObject.SetActive(false);
            mProgress3.gameObject.SetActive(false);

            ShowAnim(mPlane, () =>
            {
                FacadeWithdraw.ActionByCurWTarget((value) =>
                {
                    mProgress1.gameObject.SetActive(true);

                    mProgress1.PlayAnim(ShowBtn);
                }, (value) =>
                {
                    mProgress2.gameObject.SetActive(true);

                    float targetMoney = FacadeWithdraw.GetWTarget();
                    bool b = value >= targetMoney;
                    mP2_3_Title.text = string.Format(FacadeLanguage.GetText("10080"), FacadePayType.RegionalChange(FacadePlayer.GetMoney()));
                    mP2_4_Content.gameObject.SetActive(b);
                    mP2_4_ErrorContent.gameObject.SetActive(!b);
                    if (!b)
                        mP2_4_ErrorContent.text = string.Format(FacadeLanguage.GetText("10082"), FacadePayType.RegionalChange(value), FacadePayType.RegionalChange(targetMoney));
                    mProgress2.PlayAnim(ShowBtn, b, 3);
                }, (value) =>
                {
                    mProgress3.gameObject.SetActive(true);

                    bool b = value >= FacadeWithdraw.GetCurCheckInDay();
                    mP3_3_Title.text = string.Format(FacadeLanguage.GetText("10080"), FacadePayType.RegionalChange(FacadePlayer.GetMoney()));
                    mP3_4_Content.gameObject.SetActive(b);
                    mP3_4_ErrorContent.gameObject.SetActive(!b);
                    if (!b)
                        mP3_4_ErrorContent.text = string.Format(FacadeLanguage.GetText("10083"), GameDefines.CheckInDay, GameDefines.CheckInDay - value);
                    mProgress3.PlayAnim(ShowBtn, b, 3);
                });
            });
        }

        
        private void ShowBtn()
        {
            mExitBtn.gameObject.SetActive(true);
            mConfirmBtn.gameObject.SetActive(true);
        }


	    private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            { 
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawalProgress);
            });
        }

        private void OnConfirmBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawalProgress);
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