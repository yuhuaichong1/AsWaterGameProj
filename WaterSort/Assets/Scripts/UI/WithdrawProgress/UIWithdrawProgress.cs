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
            if(item.TargetType == WithdrawTarget.PassLevel)
                mCurMoneyText.text = FacadePayType.RegionalChange(item.WRMoney);
            else
                mCurMoneyText.text = FacadePayType.RegionalChange(FacadePlayer.GetMoney());
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
                    mProgress1.SetOrderTime(item.CreatedDate);
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
                        mP2_4_ErrorContent.text = string.Format(FacadeLanguage.GetText("10082"), FacadePayType.RegionalChange(value), FacadePayType.RegionalChange(FacadeWithdraw.GetRemainTarget()));
                    //mProgress2.PlayAnim(ShowBtn, b, 3);
                    mProgress2.SetOrderTime(item.CreatedDate);
                    mProgress2.PlayAnim(ShowBtn);
                }, (value) =>
                {
                    mProgress3.gameObject.SetActive(true);
                    bool b = value < GameDefines.CheckInDay;
                    mP3_4_Title.text = string.Format(FacadeLanguage.GetText("10080"), FacadePayType.RegionalChange(FacadePlayer.GetMoney()));
                    mP3_3_Title.text = string.Format(FacadeLanguage.GetText("10131"), FacadeWithdraw.GetCurCheckInDay(), GameDefines.CheckInDay);

                    //mP3_3_Content.gameObject.SetActive(!b);
                    //mP3_3_ErrorContent.gameObject.SetActive(b);
                    if (b)
                    {
                        mP3_3_Title.text = string.Format(FacadeLanguage.GetText("10131"), FacadeWithdraw.GetCurCheckInDay(), GameDefines.CheckInDay);
                        mP3_3_ErrorContent.text = string.Format(FacadeLanguage.GetText("10132"), GameDefines.CheckInDay, GameDefines.CheckInLevel - FacadeWithdraw.GetCurCheckLevel());
                    }
                    else
                    {
                        int remainCheckInBankDay = GameDefines.CheckInBankDay - FacadeWithdraw.GetCurCheckInBankDay();
                        int remainCheckInLevel = GameDefines.CheckInLevel - FacadeWithdraw.GetCurCheckLevel();
                        mP3_3_Title.text = string.Format(FacadeLanguage.GetText("10134"), FacadeWithdraw.GetCurCheckInBankDay(), GameDefines.CheckInBankDay);
                        if (FacadeWithdraw.GetCurCheckInBankDay() < GameDefines.CheckInBankDay)
                        {
                            mP3_3_ErrorContent.text = string.Format(FacadeLanguage.GetText("10135"), remainCheckInBankDay, remainCheckInLevel);
                        }
                        else
                        {
                            mP3_3_ErrorContent.text = string.Format(FacadeLanguage.GetText("10136"), remainCheckInLevel);
                        }
                    }
                    
                    mProgress3.SetOrderTime(item.CreatedDate);
                    mProgress3.PlayAnim(ShowBtn);

                    item.WRMoney = FacadePlayer.GetMoney();
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
                FacadeWithdraw.AfterCloseWUI();
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawProgress);
            });
        }

        private void OnConfirmBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                FacadeWithdraw.AfterCloseWUI();
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawProgress);
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