using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawMethod : BaseViewWithDraw
    {
        private static readonly Regex EmailRegex = new Regex(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase);
        private const string RetryWithdrawMissingEmailText = "PayPal收款账号不能为空";
        private Toggle toggleA;
        private Toggle toggleB;
        private Toggle toggleC;
        private GameObject email;
        private GameObject nameObj;
        private GameObject phoneFront;
        private GameObject phoneBehind;
        private InputField emailInputField;
        private InputField phoneInputField;
        private InputField nameInputField;
        private ToggleGroup toggleGroup;
        private string playerName;
        private string phone;
        private string emailAddr;
        private GameObject panelE;
        private double money;
        private int level;
        private int num;
        private DataModel dataModel;
        private Button confirmButton;
        private Button closeButton;
        private int index = 1;
        private bool uiBound;
        private bool isRetryWithdrawSubmitting;
        // 纯数字正则表达式（匹配任意长度的纯数字字符串）
        private const string OnlyNumberPattern = @"^\d+$";

        protected override void OnAwake()
        {
            BindUi();
        }

        protected override void HandleViewArgs(object[] args)
        {
            BindUi();

            dataModel = args != null && args.Length > 0 ? args[0] as DataModel : null;
            if (dataModel == null)
                dataModel = new DataModel();

            level = dataModel.Level;
            money = dataModel.Money;
            num = dataModel.Num;

            string savedName = string.IsNullOrEmpty(dataModel.Name) ? GameManagerWZ.instance.getPlayerName() : dataModel.Name;
            string savedPhone = string.IsNullOrEmpty(dataModel.Phone) ? GameManagerWZ.instance.getPlayerPhone() : dataModel.Phone;
            string savedEmail = string.IsNullOrEmpty(dataModel.Email) ? GameManagerWZ.instance.getPlayerEmail() : dataModel.Email;
            int savedIndex = 3;

            playerName = savedName ?? string.Empty;
            phone = savedPhone ?? string.Empty;
            emailAddr = SanitizeEmailInput(savedEmail);

            dataModel.Name = playerName;
            dataModel.Phone = phone;
            dataModel.Email = emailAddr;
            dataModel.Index = 3;

            SetMethodSelection(savedIndex, false);

            nameInputField?.SetTextWithoutNotify(playerName);
            phoneInputField?.SetTextWithoutNotify(phone);
            emailInputField?.SetTextWithoutNotify(emailAddr);
        }

        private void SetMethodSelection(int selectedIndex, bool clearInactiveFields)
        {
            BindUi();

            index = Mathf.Clamp(selectedIndex, 1, 3);

            if (toggleA != null)
                toggleA.SetIsOnWithoutNotify(index == 1);
            if (toggleB != null)
                toggleB.SetIsOnWithoutNotify(index == 2);
            if (toggleC != null)
                toggleC.SetIsOnWithoutNotify(index == 3);

            bool isEmailMethod = index == 3;
            if (email == null || phoneFront == null || phoneBehind == null)
            {
                Debug.LogError("WithDrawMethod UI binding failed: PanelA/PanelC/Email, phoneFront or phoneBehind is missing.");
                return;
            }

            email.SetActive(isEmailMethod);
            phoneFront.SetActive(!isEmailMethod);
            phoneBehind.SetActive(!isEmailMethod);

            if (!clearInactiveFields)
                return;

            if (isEmailMethod)
            {
                if (phoneInputField != null)
                    phoneInputField.SetTextWithoutNotify(string.Empty);
                phone = string.Empty;
            }
            else
            {
                if (emailInputField != null)
                    emailInputField.SetTextWithoutNotify(string.Empty);
                emailAddr = string.Empty;
            }
        }

        private void BindUi()
        {
            if (uiBound)
                return;

            toggleA = FindComponent<Toggle>("PanelA/PanelB/ToggleA");
            toggleB = FindComponent<Toggle>("PanelA/PanelB/ToggleB");
            toggleC = FindComponent<Toggle>("PanelA/PanelB/ToggleC");
            email = FindObject("PanelA/PanelC/Email");
            phoneFront = FindObject("PanelA/PanelC/phoneFront");
            phoneBehind = FindObject("PanelA/PanelC/phoneBehind");
            nameObj = FindObject("PanelA/PanelC/Name");
            toggleGroup = FindComponent<ToggleGroup>("PanelA/PanelB");
            emailInputField = FindComponent<InputField>("PanelA/PanelC/Email");
            phoneInputField = FindComponent<InputField>("PanelA/PanelC/phoneBehind");
            nameInputField = FindComponent<InputField>("PanelA/PanelC/Name");
            panelE = FindObject("PanelE");

            if (toggleA != null)
            {
                toggleA.onValueChanged.RemoveListener(OnToggleValueChangeA);
                toggleA.onValueChanged.AddListener(OnToggleValueChangeA);
                toggleA.interactable = false;
            }

            if (toggleB != null)
            {
                toggleB.onValueChanged.RemoveListener(OnToggleValueChangeB);
                toggleB.onValueChanged.AddListener(OnToggleValueChangeB);
                toggleB.interactable = false;
            }

            if (toggleC != null)
            {
                toggleC.onValueChanged.RemoveListener(OnToggleValueChangeC);
                toggleC.onValueChanged.AddListener(OnToggleValueChangeC);
                toggleC.interactable = true;
            }

            if (emailInputField != null)
            {
                emailInputField.onValueChanged.RemoveListener(OnInputValueChangeEmail);
                emailInputField.onValueChanged.AddListener(OnInputValueChangeEmail);
                emailInputField.onValidateInput -= ValidateEmailCharacter;
                emailInputField.onValidateInput += ValidateEmailCharacter;
            }

            if (phoneInputField != null)
            {
                phoneInputField.onValueChanged.RemoveListener(OnInputValueChangePhone);
                phoneInputField.onValueChanged.AddListener(OnInputValueChangePhone);
            }

            if (nameInputField != null)
            {
                nameInputField.onValueChanged.RemoveListener(OnInputValueChangeName);
                nameInputField.onValueChanged.AddListener(OnInputValueChangeName);
            }

            confirmButton = FindComponent<Button>("PanelA/Confirm");
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnButtonClick);
                confirmButton.onClick.AddListener(OnButtonClick);
            }

            closeButton = FindComponent<Button>("PanelA/close");
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseBtnClick);
                closeButton.onClick.AddListener(OnCloseBtnClick);
            }

            Button quesButton = FindComponent<Button>("PanelA/Panel/ques");
            if (quesButton != null)
            {
                quesButton.onClick.RemoveListener(OnButtonClickQues);
                quesButton.onClick.AddListener(OnButtonClickQues);
            }

            uiBound = email != null && phoneFront != null && phoneBehind != null;
        }

        private GameObject FindObject(string path)
        {
            Transform child = transform.Find(path);
            if (child == null)
            {
                Debug.LogError($"WithDrawMethod missing UI path: {path}");
                return null;
            }

            return child.gameObject;
        }

        private T FindComponent<T>(string path) where T : Component
        {
            GameObject obj = FindObject(path);
            if (obj == null)
                return null;

            T component = obj.GetComponent<T>();
            if (component == null)
                Debug.LogError($"WithDrawMethod missing component {typeof(T).Name} at path: {path}");

            return component;
        }

        public void OnToggleValueChangeA(bool value)
        {
            if (value)
            {
                SetMethodSelection(3, false);
            }
        }

        public void OnToggleValueChangeB(bool value)
        {
            if (value)
            {
                SetMethodSelection(3, false);
            }
        }

        public void OnToggleValueChangeC(bool value)
        {
            if (value)
            {
                SetMethodSelection(3, true);
            }
        }

        public void OnInputValueChangeName(string value)
        {
            playerName = value;
        }

        public void OnInputValueChangePhone(string value)
        {
            phone = value;
        }

        public void OnInputValueChangeEmail(string value)
        {
            string sanitizedValue = SanitizeEmailInput(value);
            if (sanitizedValue != value && emailInputField != null && emailInputField.text != sanitizedValue)
            {
                emailInputField.SetTextWithoutNotify(sanitizedValue);
            }

            emailAddr = sanitizedValue;
        }

        public void OnButtonClick()
        {
            if (string.IsNullOrEmpty(emailAddr))
            {
                panelE.SetActive(true);
                StartCoroutine(DelayedAction());
                Debug.Log("邮件为空！");
                return;
            }

            string cleanEmail = SanitizeEmailInput(emailAddr);
            if (!IsValidComEmail(cleanEmail))
            {
                panelE.SetActive(true);
                StartCoroutine(DelayedAction());
                Debug.Log("邮箱格式错误！");
                return;
            }

            emailAddr = cleanEmail;
            if (emailInputField != null && emailInputField.text != cleanEmail)
            {
                emailInputField.SetTextWithoutNotify(cleanEmail);
            }

            dataModel.Email = emailAddr;
            dataModel.Phone = string.Empty;
            dataModel.Index = 3;
            GameManagerWZ.instance.SavePlayerEmail(emailAddr);
            GameManagerWZ.instance.SavePlayerIndex(3);

            if (ShouldSubmitRetryWithdrawOnExit())
            {
                SubmitRetryWithdrawAndClose();
                return;
            }

            if (ShouldOpenProcessViewOnMethodExit())
            {
                GameApp.viewManager.Open(ViewType.WithDrawProcess1, dataModel);
                GameApp.viewManager.Close(ViewId);
                return;
            }

            // Goal2 绑邮箱确认后：进 Mission，不进 Confirm/Process1。
            if (dataModel != null && (dataModel.Num == 2
                || (GameManagerWZ.instance != null && GameManagerWZ.instance.Stage2CashGuidePending)))
            {
                GameApp.viewManager.Close(ViewId);
                GameManagerWZ.instance?.FinishStage2CashGuideAndShowMission();
                return;
            }

            GameApp.viewManager.Open(ViewType.WithDrawConfirm, dataModel);
            GameApp.viewManager.Close(ViewId);
        }

        IEnumerator DelayedAction()
        {
            yield return new WaitForSeconds(2.0f); // 等待2秒
            panelE.SetActive(false);
        }

        public void OnButtonClickQues()
        {
            GameApp.viewManager.Open(ViewType.WithDrawQues);
        }

        public void OnCloseBtnClick()
        {
            if (ShouldSubmitRetryWithdrawOnExit())
            {
                SyncEmailForRetrySubmit(false);
                SubmitRetryWithdrawAndClose();
                return;
            }

            if (ShouldOpenProcessViewOnMethodExit())
            {
                SyncEmailForProcessView();
                GameApp.viewManager.Open(ViewType.WithDrawProcess1, dataModel);
                GameApp.viewManager.Close(ViewId);
                return;
            }

            // Goal2：关闭绑邮箱页也进 Mission（与视频一致：引导完 → Mission → 关卡）。
            if (dataModel != null && (dataModel.Num == 2
                || (GameManagerWZ.instance != null && GameManagerWZ.instance.Stage2CashGuidePending)))
            {
                GameApp.viewManager.Close(ViewId);
                GameManagerWZ.instance?.FinishStage2CashGuideAndShowMission();
                return;
            }

            if (ShouldCloseWithoutAdvance())
            {
                GameApp.viewManager.Close(ViewId);
                return;
            }

            StartCoroutine(DelayedNextLevel());
        }

        private bool ShouldCloseWithoutAdvance()
        {
            return dataModel == null
                   || dataModel.CloseWithoutAdvance
                   || dataModel.Special == DataModel.SpecialWithdrawMissionBindEmail
                   || dataModel.Special == DataModel.SpecialLuckyWalletWithdraw
                   || dataModel.RetryWithdraw
                   || GameManagerWZ.instance == null;
        }

        private bool ShouldOpenProcessViewOnMethodExit()
        {
            return dataModel != null
                   && dataModel.UseProcessViewOnMethodExit;
        }

        private bool ShouldSubmitRetryWithdrawOnExit()
        {
            return dataModel != null
                   && dataModel.RetryWithdraw
                   && dataModel.SubmitRetryWithdrawOnMethodExit;
        }

        private void SyncEmailForProcessView()
        {
            if (dataModel == null || GameManagerWZ.instance == null)
                return;

            string cleanEmail = SanitizeEmailInput(emailAddr);
            if (!string.IsNullOrEmpty(cleanEmail) && IsValidComEmail(cleanEmail))
            {
                emailAddr = cleanEmail;
                dataModel.Email = cleanEmail;
                dataModel.Phone = string.Empty;
                dataModel.Index = 3;
                GameManagerWZ.instance.SavePlayerEmail(cleanEmail);
                GameManagerWZ.instance.SavePlayerIndex(3);
                return;
            }

            string fallbackEmail = !string.IsNullOrEmpty(dataModel.Email)
                ? dataModel.Email
                : GameManagerWZ.instance.getPlayerEmail();
            dataModel.Email = string.IsNullOrEmpty(fallbackEmail) ? string.Empty : fallbackEmail;
            dataModel.Phone = string.Empty;
            dataModel.Index = 3;
        }

        private bool SyncEmailForRetrySubmit(bool requireValidInput)
        {
            if (dataModel == null)
                return false;

            string cleanEmail = SanitizeEmailInput(emailAddr);
            if (!string.IsNullOrEmpty(cleanEmail) && IsValidComEmail(cleanEmail))
            {
                emailAddr = cleanEmail;
                dataModel.Email = cleanEmail;
                dataModel.Receiver = cleanEmail;
                dataModel.Phone = string.Empty;
                dataModel.Index = 3;
                if (GameManagerWZ.instance != null)
                {
                    GameManagerWZ.instance.SavePlayerEmail(cleanEmail);
                    GameManagerWZ.instance.SavePlayerIndex(3);
                }

                if (emailInputField != null && emailInputField.text != cleanEmail)
                    emailInputField.SetTextWithoutNotify(cleanEmail);

                return true;
            }

            if (requireValidInput)
                return false;

            string fallbackEmail = !string.IsNullOrEmpty(dataModel.Email)
                ? dataModel.Email
                : (GameManagerWZ.instance != null ? GameManagerWZ.instance.getPlayerEmail() : string.Empty);
            fallbackEmail = SanitizeEmailInput(fallbackEmail);
            if (string.IsNullOrEmpty(fallbackEmail) || !IsValidComEmail(fallbackEmail))
                return false;

            emailAddr = fallbackEmail;
            dataModel.Email = fallbackEmail;
            dataModel.Receiver = fallbackEmail;
            dataModel.Phone = string.Empty;
            dataModel.Index = 3;
            if (GameManagerWZ.instance != null)
            {
                GameManagerWZ.instance.SavePlayerEmail(fallbackEmail);
                GameManagerWZ.instance.SavePlayerIndex(3);
            }
            if (emailInputField != null && emailInputField.text != fallbackEmail)
                emailInputField.SetTextWithoutNotify(fallbackEmail);

            return true;
        }

        private void SubmitRetryWithdrawAndClose()
        {
            if (isRetryWithdrawSubmitting)
                return;

            if (!SyncEmailForRetrySubmit(false))
            {
                //UIManager.instance?.ShowView(EUIType.NoticeView, RetryWithdrawMissingEmailText);
                GameApp.viewManager.Close(ViewId);
                return;
            }

            ClientWithdrawRequest request = BuildRetryWithdrawRequest();
            if (request == null)
            {
               // UIManager.instance?.ShowView(EUIType.NoticeView, "提现参数不能为空");
                GameApp.viewManager.Close(ViewId);
                return;
            }

            isRetryWithdrawSubmitting = true;
            SetSubmitButtonsInteractable(false);
            if (dataModel != null)
            {
                dataModel.SubmitRetryWithdrawOnMethodExit = false;
                dataModel.UseProcessViewOnMethodExit = false;
            }

            MonoBehaviour runner = GameManagerWZ.instance != null ? (MonoBehaviour)GameManagerWZ.instance : this;
            runner.StartCoroutine(WithdrawApiService.SubmitWithdraw(request, OnRetryWithdrawSubmitSuccess, OnRetryWithdrawSubmitError));
            GameApp.viewManager.Close(ViewId);
        }

        private ClientWithdrawRequest BuildRetryWithdrawRequest()
        {
            if (dataModel == null)
                return null;

            if (string.IsNullOrEmpty(dataModel.OrderNo))
                return null;

            string receiver = !string.IsNullOrEmpty(dataModel.Email)
                ? dataModel.Email
                : dataModel.Receiver;
            if (string.IsNullOrEmpty(receiver))
                return null;

            string withdrawType = ResolveRetryWithdrawType();
            return new ClientWithdrawRequest
            {
                OrderNo = dataModel.OrderNo,
                WithdrawType = withdrawType,
                Receiver = receiver,
                Amount = dataModel.Money > 0d ? (double?)System.Math.Round(dataModel.Money, 2) : null,
                Currency = WithdrawConstants.CurrencyUsd,
                Note = string.Equals(withdrawType, WithdrawConstants.WithdrawTypeWallet, System.StringComparison.OrdinalIgnoreCase)
                    ? "Wallet withdraw"
                    : "First ad withdraw",
                EmailSubject = string.Empty,
                RecipientType = WithdrawConstants.RecipientTypeEmail
            };
        }

        private string ResolveRetryWithdrawType()
        {
            if (dataModel != null && !string.IsNullOrEmpty(dataModel.WithdrawType))
                return dataModel.WithdrawType;

            return dataModel != null && dataModel.Special == DataModel.SpecialLuckyWalletWithdraw
                ? WithdrawConstants.WithdrawTypeWallet
                : WithdrawConstants.WithdrawTypeFirstAd;
        }

        private void OnRetryWithdrawSubmitSuccess(WithdrawOrderData order)
        {
            isRetryWithdrawSubmitting = false;
            SetSubmitButtonsInteractable(true);
            if (dataModel == null || order == null)
                return;

            dataModel.OrderNo = order.OrderNo;
            dataModel.OrderStatus = order.OrderStatus;
            dataModel.WithdrawType = order.WithdrawType;
            dataModel.Receiver = order.Receiver;
            dataModel.WalletFrozen = order.WalletFrozen;
        }

        private void OnRetryWithdrawSubmitError(string error)
        {
            isRetryWithdrawSubmitting = false;
            SetSubmitButtonsInteractable(true);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"Retry withdraw request failed: {error}");
                WithdrawApiService.LogPaymentNotice("WithDrawMethod", error, LogType.Warning);
            }
        }

        private void SetSubmitButtonsInteractable(bool interactable)
        {
            if (confirmButton != null)
                confirmButton.interactable = interactable;

            if (closeButton != null)
                closeButton.interactable = interactable;
        }

        private char ValidateEmailCharacter(string text, int charIndex, char addedChar)
        {
            if (char.IsWhiteSpace(addedChar))
            {
                return '\0';
            }

            return IsAllowedEmailCharacter(addedChar) ? addedChar : '\0';
        }

        private string SanitizeEmailInput(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int bufferIndex = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (IsAllowedEmailCharacter(current))
                {
                    buffer[bufferIndex++] = current;
                }
            }

            return new string(buffer, 0, bufferIndex).Trim();
        }

        private bool IsAllowedEmailCharacter(char value)
        {
            return char.IsLetterOrDigit(value)
                   || value == '@'
                   || value == '.'
                   || value == '_'
                   || value == '-'
                   || value == '+'
                   || value == '%';
        }

        private bool IsValidComEmail(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return EmailRegex.IsMatch(value);
        }

        private System.Collections.IEnumerator DelayedNextLevel()
        {
            yield return new WaitForSeconds(0.2f);
            var gm = GameManagerWZ.instance;
            if (gm != null)
            {
                if (gm.Stage2CashGuidePending || gm.DeferHostLevelLoad
                    || (dataModel != null && dataModel.Num == 2))
                {
                    GameApp.viewManager.Close(ViewId);
                    gm.FinishStage2CashGuideAndShowMission();
                    yield break;
                }

                gm.NextLevel();
            }
            GameApp.viewManager.Close(ViewId);
        }
    }
}
