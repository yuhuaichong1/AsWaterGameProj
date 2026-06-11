
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawEnterInfo : BaseUI
    {
        private List<PayNode> payTypes;
        private EPayType ePayType;
        private EPOEType infoType;
        private InputField msgInputField;

        private string inputName;
        private string inputMsg;

        private WithdrawalRecordItem UIProgressTarget;

        protected override void OnSetParam(params object[] args)
        {
            UIProgressTarget  = (WithdrawalRecordItem)args[0];
        }

        protected override void OnAwake()
        {
            payTypes = FacadePayType.GetPayItems();

            InitShow();
        }

        /// <summary>
        /// 初始化显示
        /// </summary>
        private void InitShow()
        {
            bool b1 = payTypes.Count >= 1;
            mPayType1Toggle.gameObject.SetActive(b1);
            if (b1)
                mPY1Icon.sprite = payTypes[0].picture;

            bool b2 = payTypes.Count >= 2;
            mPayType2Toggle.gameObject.SetActive(b2);
            if (b2)
                mPY2Icon.sprite = payTypes[1].picture;

            bool b3 = payTypes.Count >= 3;
            mPayType3Toggle.gameObject.SetActive(b3);
            if (b3)
                mPY3Icon.sprite = payTypes[2].picture;

            bool b4 = payTypes.Count >= 4;
            mPayType4Toggle.gameObject.SetActive(b4);
            if (b4)
                mPY4Icon.sprite = payTypes[3].picture;

            bool b5 = payTypes.Count >= 5;
            mPayType5Toggle.gameObject.SetActive(b5);
            if (b5)
                mPY5Icon.sprite = payTypes[4].picture;

            bool b6 = payTypes.Count >= 6;
            mPayType6Toggle.gameObject.SetActive(b6);
            if (b6)
                mPY6Icon.sprite = payTypes[5].picture;

            mAreaCodeText.text = $"+{FacadePayType.GetNANP?.Invoke()}";

            mNameInput.placeholder.gameObject.GetComponent<Text>().text = FacadeLanguage.GetText("10056");
            mAddressInput.placeholder.gameObject.GetComponent<Text>().text = FacadeLanguage.GetText("10058");
            mPhoneInput.placeholder.gameObject.GetComponent<Text>().text = FacadeLanguage.GetText("10057");
            mAddressOrPhoneInput.placeholder.gameObject.GetComponent<Text>().text = FacadeLanguage.GetText("10059");
        }

        protected override void OnEnable() 
        {
            ShowAnim(mPlane);
            ClearData();
        }

        /// <summary>
        /// 清除填写的数据并初始化
        /// </summary>
        private void ClearData()
        {
            ePayType = payTypes[0].payType;
            mPayType1Toggle.isOn = true;
            infoType = payTypes[0].infoType;
            ShowInputFiled(infoType);

            mNameInput.text = "";
            mAddressInput.text = "";
            mPhoneInput.text = "";
            mAddressOrPhoneInput.text = "";
        }

        #region 按钮/开关回调

        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                FacadeWithdraw.AfterCloseWUI();
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawEnterInfo);
            });
        }

        private void OnHelpBtnClickHandle()
        {
            UIManager.Instance.OpenAsync<UIWithdrawFeedback>(EUIType.EUIWithdrawFeedback);
        }

        private void OnConfirmBtnClickHandle()
        {
            inputName = mNameInput.text;
            inputMsg = msgInputField.text;

            if (string.IsNullOrEmpty(inputName)) 
            {
                UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10027"));
                return;
            }

            if(string.IsNullOrEmpty(inputMsg)) 
            {
                CheckNotice();
                
                return;
            }
            else
            {
                if(CheckAOPMsg())
                {
                    FacadeWithdraw.SetWName(inputName);
                    FacadeWithdraw.SetPayType(ePayType);
                    FacadeWithdraw.SetWPhoneOrEmail(inputMsg);

                    HideAnim(mPlane, () =>
                    {
                        UIManager.Instance.CloseUI(EUIType.EUIWithdrawEnterInfo);
                        UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIWithdrawConfirm, UIOpenType.None, null, UIProgressTarget);
                    });
                }
            }
        }

        private void OnPayType1ToggleValueChanged(bool arg0)
        {
            if(arg0) 
            {
                ePayType = payTypes[0].payType;
                infoType = payTypes[0].infoType;
                ShowInputFiled(infoType);

                D.Error(ePayType.ToString());
            }
        }

        private void OnPayType2ToggleValueChanged(bool arg0)
        {
            if (arg0)
            {
                ePayType = payTypes[1].payType;
                infoType = payTypes[1].infoType;
                ShowInputFiled(infoType);

                D.Error(ePayType.ToString());
            }
        }

        private void OnPayType3ToggleValueChanged(bool arg0)
        {
            if (arg0)
            {
                ePayType = payTypes[2].payType;
                infoType = payTypes[2].infoType;
                ShowInputFiled(infoType);

                D.Error(ePayType.ToString());
            }
        }

        private void OnPayType4ToggleValueChanged(bool arg0)
        {
            if (arg0)
            {
                ePayType = payTypes[3].payType;
                infoType = payTypes[3].infoType;
                ShowInputFiled(infoType);

                D.Error(ePayType.ToString());
            }
        }

        private void OnPayType5ToggleValueChanged(bool arg0)
        {
            if (arg0)
            {
                ePayType = payTypes[4].payType;
                infoType = payTypes[4].infoType;
                ShowInputFiled(infoType);

                D.Error(ePayType.ToString());
            }
        }

        private void OnPayType6ToggleValueChanged(bool arg0)
        {
            if (arg0)
            {
                ePayType = payTypes[5].payType;
                infoType = payTypes[5].infoType;
                ShowInputFiled(infoType);

                D.Error(ePayType.ToString());
            }
        }

        #endregion

        /// <summary>
        /// 显示指定的输入框
        /// </summary>
        /// <param name="infoType">指定的输入框</param>
        private void ShowInputFiled(EPOEType infoType)
        {
            mAddressInput.gameObject.SetActive(infoType == EPOEType.Email);
            mPhone.gameObject.SetActive(infoType == EPOEType.Phone);
            mAddressOrPhoneInput.gameObject.SetActive(infoType == EPOEType.POE);

            switch(infoType) 
            { 
                case EPOEType.Email:
                    msgInputField = mAddressInput;
                    break;
                case EPOEType.Phone:
                    msgInputField = mPhoneInput;
                    break;
                case EPOEType.POE:
                    msgInputField = mAddressOrPhoneInput;
                    break;
            }
        }

        /// <summary>
        /// 下输入框空值时应该出现哪个通知
        /// </summary>
        private void CheckNotice()
        {
            switch (infoType)
            {
                case EPOEType.Email:
                    UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10058"));
                    break;
                case EPOEType.Phone:
                    UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10057"));
                    break;
                case EPOEType.POE:
                    UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10059"));
                    break;
            }
        }

        /// <summary>
        /// 指定的输入框的取值是否合规
        /// </summary>
        /// <returns>取值是否合规</returns>
        private bool CheckAOPMsg()
        {
            bool b = true;
            string lauguageId = "";
            switch (infoType) 
            {
                case EPOEType.Email:
                    b = msgInputField.text.IfEmail();
                    lauguageId = "10061";
                    break;
                case EPOEType.Phone:
                    b = msgInputField.text.IfPhoneNumber();
                    lauguageId = "10060";
                    break;
                case EPOEType.POE:
                    b = msgInputField.text.IfEmail() || msgInputField.text.IfPhoneNumber();
                    lauguageId = "10062";
                    break;
            }

            if (!b)
                UIManager.Instance.OpenNotice2(FacadeLanguage.GetText(lauguageId));

            return b;
        }

        protected override void OnDisable()
        {
        
        }

        protected override void OnDispose()
        {
            payTypes = null;
            msgInputField = null;
        }
    }
}