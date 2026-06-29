using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class GM : MonoBehaviour
{
    public Button GMBtn;
    public GameObject GMPlane;
    [Space]
    public Button MoneyBtn;
    public InputField MoneyField;
    [Space]
    public Button EnergyBtn;
    public InputField EnergyField;
    [Space]
    public Button PropBtn;
    public Dropdown PropDropdown;
    [Space]
    public Button SkipLevelBtn;
    public InputField SkipLevelField;
    [Space]
    public Button PassLevelBtn;
    [Space]
    public Button TestFunctionBtn;
    [Space]
    public Button SimulateAdBtn;
    [Space]
    public InputField SkipTime;
    public Button BtnSkipStepTime;
    public Button BtnJumpToFirstStep;

    public bool GMbool;

    void Awake()
    {
        GMbool = false;

        if (!GameDefines.ifDebug)
        {
            gameObject.SetActive(false);
            return;
        }

        if (GMBtn != null && GMPlane != null)
        {
            GMPlane.gameObject.SetActive(GMbool);
            GMBtn.onClick.AddListener(OnGMBtnClick);
        }

        if (MoneyBtn != null && MoneyField != null)
            MoneyBtn.onClick.AddListener(OnMoneyBtnClick);

        if (EnergyBtn != null && EnergyField != null)
            EnergyBtn.onClick.AddListener(OnEnergyBtnClick);

        if (PropBtn != null && PropDropdown != null)
            PropBtn.onClick.AddListener(OnPropBtnClick);

        if (SkipLevelBtn != null && SkipLevelField != null)
            SkipLevelBtn.onClick.AddListener(OnSkipLevelBtnClick);

        if (PassLevelBtn != null)
            PassLevelBtn.onClick.AddListener(OnPassLevelBtnClick);

        if (TestFunctionBtn != null)
            TestFunctionBtn.onClick.AddListener(OnTestFunctionBtnClick);

        if (SimulateAdBtn != null)
            SimulateAdBtn.onClick.AddListener(OnSimulateAdBtnClick);

        if (BtnSkipStepTime != null)
            BtnSkipStepTime.onClick.AddListener(OnBtnSkipStepTimeClick);

        if (BtnJumpToFirstStep != null)
            BtnJumpToFirstStep.onClick.AddListener(OnBtnJumpToFirstStepClick);
    }

    void Update()
    {
        if (!IsGmPanelOpen())
            return;

        if (GameDefines.UsePayoutV2 && Input.GetKeyDown(KeyCode.N))
            FacadePayout.GM_SkipCountdown?.Invoke();
    }

    private bool IsGmPanelOpen()
    {
        return GameDefines.ifDebug && GMbool && GMPlane != null && GMPlane.activeSelf;
    }

    private void OnGMBtnClick()
    {
        GMbool = !GMbool;
        GMPlane.gameObject.SetActive(GMbool);
    }

    private void OnMoneyBtnClick()
    {
        if (double.TryParse(MoneyField.text, out double money))
        {
            FacadePlayer.AddMoney(money);
            FacadeGamePlay.SetCurMoneyShow();
        }
    }

    private void OnEnergyBtnClick()
    {
        if (int.TryParse(EnergyField.text, out int energy))
            FacadePlayer.AddEnergy(energy);
    }

    private void OnPropBtnClick()
    {
        switch (PropDropdown.value)
        {
            case 0:
                FacadeGamePlay.SetProp1CountShow();
                break;
            case 1:
                FacadeGamePlay.SetProp2CountShow();
                break;
            case 2:
                FacadeGamePlay.SetProp3CountShow();
                break;
        }
    }

    private void OnSkipLevelBtnClick()
    {
        UIManager.Instance.OpenAsync<UILevelCompleted>(EUIType.EUILevelCompleted, UIOpenType.None, null, FacadePlayer.GetLevel());
        FacadePlayer.SetLevel(int.Parse(SkipLevelField.text));
    }

    private void OnPassLevelBtnClick()
    {
        UIManager.Instance.OpenAsync<UILevelCompleted>(EUIType.EUILevelCompleted, UIOpenType.None, null, FacadePlayer.GetLevel());
        FacadePlayer.AddLevel(1);
    }

    private void OnSimulateAdBtnClick()
    {
        GmSimulateAdComplete();
    }

    private void OnBtnSkipStepTimeClick()
    {
        if (!GameDefines.ifDebug || !GameDefines.UsePayoutV2)
            return;

        if (SkipTime == null || !int.TryParse(SkipTime.text, out int minutes) || minutes <= 0)
        {
            D.Log("[GM] 请输入有效的缩短分钟数");
            return;
        }

        FacadePayout.GM_ShortenStepTimeMinutes?.Invoke(minutes);
    }

    private void OnBtnJumpToFirstStepClick()
    {
        if (!GameDefines.ifDebug || !GameDefines.UsePayoutV2)
            return;

        FacadePayout.GM_JumpToStep?.Invoke(1);
        D.Log("[GM] 已还原到提现步骤第一步");
    }

    private void GmSimulateAdComplete()
    {
        if (!GameDefines.ifDebug || !GameDefines.UsePayoutV2)
            return;

        FacadeAd.OnRewardAdReceivedReward?.Invoke("gm", 0, 0, string.Empty, 1);
        D.Log("[GM] 已模拟激励广告完成");
    }

    private void OnTestFunctionBtnClick()
    {
        UIManager.Instance.OpenAsync<UILuckyReward>(EUIType.EUILuckyReward);

        //FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
        //{
        //    Type = ERewardType.Prop1,
        //    Count = 1f,
        //}, null);
    }
}
