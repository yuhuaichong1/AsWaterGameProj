using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class WOListItem : MonoBehaviour
{
    public Image Icon;
    public Text TargetText;
    public Slider MoneySlider;
    public Text OtherText;
    public Button CashOutBtn;
    public Button ContinueBtn;
    public Button UnContinueBtn;
    public GameObject ProgressObj;
    public Text PTime;

    private PayoutEntryKey entryKey;
    private float targetAmount;
    private System.Action onRefreshParent;

    void Awake()
    {
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
        CancelInvoke(nameof(Refresh));
        InvokeRepeating(nameof(Refresh), 1f, 1f);
        Refresh();
    }

    public void Refresh()
    {
        double curMoney = FacadePlayer.GetMoney();
        MoneySlider.value = targetAmount <= 0 ? 0 : Mathf.Clamp01((float)(curMoney / targetAmount));
        OtherText.text = $"{FacadePayType.RegionalChange((float)curMoney)} / {FacadePayType.RegionalChange(targetAmount)}";

        bool started = FacadePayout.HasActiveEntry(entryKey);
        bool canContinue = FacadePayout.CanContinue(entryKey);

        CashOutBtn.gameObject.SetActive(!started);
        ContinueBtn.gameObject.SetActive(started && canContinue);
        UnContinueBtn.gameObject.SetActive(started && !canContinue);
        ProgressObj.SetActive(started);

        if (started)
        {
            long remain = FacadePayout.GetRemainSeconds(entryKey);
            PTime.text = string.Format(FacadeLanguage.GetText("10233"), PayoutStepTaskHelper.FormatRemainTime(remain));
        }
    }

    private void OnCashOutBtnClick()
    {
        if (FacadePayout.EnsureStartedAndOpen(entryKey))
            onRefreshParent?.Invoke();
    }

    private void OnContinueBtnClick()
    {
        ModuleMgr.Instance.PayoutModule.SetGmFocusKey(entryKey);
        FacadePayout.OpenProgressPanel(entryKey);
    }

    private void OnUnContinueBtnClick()
    {
        ModuleMgr.Instance.PayoutModule.SetGmFocusKey(entryKey);
        FacadePayout.OpenProgressPanel(entryKey);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(Refresh));
    }
}
