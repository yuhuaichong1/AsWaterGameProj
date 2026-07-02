using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class WOListItem : MonoBehaviour
{
    public Image Icon;
    public Text TargetText;
    public Slider MoneySlider;
    public Text MSText;
    public Text OtherText;
    public Button CashOutBtn;
    public Button ContinueBtn;
    public Button UnContinueBtn;
    public GameObject ProgressObj;
    public Text PTime;

    private PayoutEntryKey entryKey;
    private float targetAmount;
    private System.Action onRefreshParent;
    private Sprite icon;
    void Awake()
    {
        if (MSText == null && MoneySlider != null)
            MSText = MoneySlider.transform.Find("MSText")?.GetComponent<Text>();

        CashOutBtn.onClick.AddListener(OnCashOutBtnClick);
        ContinueBtn.onClick.AddListener(OnContinueBtnClick);
        UnContinueBtn.onClick.AddListener(OnUnContinueBtnClick);
    }

    public void Init(PayoutEntryKey key, Sprite icon, float target, System.Action refreshParent)
    {
        entryKey = key;
        targetAmount = target;
        onRefreshParent = refreshParent;
        Icon.sprite = icon;
        TargetText.text = FacadePayType.RegionalChange(target);

        this.icon = icon;
        CancelInvoke(nameof(Refresh));
        InvokeRepeating(nameof(Refresh), 0f, 1f);
        Refresh();
    }

    public void Refresh()
    {
        var entry = FacadePayout.GetEntry?.Invoke(entryKey);
        bool started = entry != null && entry.IsStarted;
        bool showTaskUi = started && entry.panelAcknowledged;
        bool awaitingPanelClose = started && !entry.panelAcknowledged;
        bool showBalanceProgress = !started || awaitingPanelClose;
        bool canContinue = started && FacadePayout.CanContinue(entryKey);
        bool canCashOut = !started && FacadePayout.CanStart != null && FacadePayout.CanStart(entryKey);

        double curMoney = FacadePlayer.GetMoney();
        string balanceProgressText =
            $"{FacadePayType.RegionalChange((float)curMoney)} / {FacadePayType.RegionalChange(targetAmount)}";

        MoneySlider.gameObject.SetActive(showBalanceProgress);
        if (MSText != null)
        {
            MSText.gameObject.SetActive(showBalanceProgress);
            if (showBalanceProgress)
                MSText.text = balanceProgressText;
        }

        if (showBalanceProgress)
            MoneySlider.value = targetAmount <= 0 ? 0 : Mathf.Clamp01((float)(curMoney / targetAmount));

        if (OtherText != null)
        {
            OtherText.gameObject.SetActive(showTaskUi);
            if (showTaskUi)
                OtherText.text = FacadePayout.GetStepDisplay(entryKey).CurTask;
        }

        CashOutBtn.gameObject.SetActive(!started);
        //if (!started)
        //    CashOutBtn.interactable = canCashOut;

        ContinueBtn.gameObject.SetActive(showTaskUi && canContinue);
        UnContinueBtn.gameObject.SetActive(showTaskUi && !canContinue);
        ProgressObj.SetActive(showTaskUi);

        if (PTime != null)
        {
            if (showTaskUi)
            {
                long remain = FacadePayout.GetRemainSeconds(entryKey);
                bool showTime = remain > 0;
                PTime.gameObject.SetActive(showTime);
                if (showTime)
                    PTime.text = PayoutStepTaskHelper.FormatListCountdown(remain);
            }
            else
            {
                PTime.gameObject.SetActive(false);
            }
        }
    }

    private void OnCashOutBtnClick()
    {
        if (FacadePayout.CanStart != null && !FacadePayout.CanStart(entryKey))
        {
            string s1 = FacadePayType.RegionalChange(targetAmount - FacadePlayer.GetMoney());
            string s2 = FacadePayType.RegionalChange(targetAmount);

            UIManager.Instance.OpenNotice3(string.Format(FacadeLanguage.GetText("10142"), s1, s2));
            return;
        }

        if (!string.IsNullOrEmpty(FacadeWithdraw.GetWPhoneOrEmail2(entryKey.Channel)))

            UIManager.Instance.OpenAsync<UIWithdrawConfirm2>(EUIType.EUIWithdrawConfirm2, UIOpenType.None, null, entryKey, onRefreshParent, targetAmount, icon);

        else

            UIManager.Instance.OpenNotice3(FacadeLanguage.GetText("10059"));

    }

    private void OnCashOutBtnClick2()
    {

        if (FacadePayout.EnsureStartedAndOpen(entryKey))
            onRefreshParent?.Invoke();
    }

    private void OnContinueBtnClick()
    {
        if (!FacadePayout.CanContinue(entryKey)) return;

        FacadePayout.ContinueStep(entryKey);
        ModuleMgr.Instance.PayoutModule.SetGmFocusKey(entryKey);
        FacadePayout.OpenProgressPanel(entryKey);
        onRefreshParent?.Invoke();
    }

    private void OnUnContinueBtnClick()
    {
        var entry = FacadePayout.GetEntry?.Invoke(entryKey);
        if (entry == null || !entry.IsStarted) return;

        ModuleMgr.Instance.PayoutModule.SetGmFocusKey(entryKey);
        FacadePayout.OpenProgressPanel(entryKey);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(Refresh));
    }
}
