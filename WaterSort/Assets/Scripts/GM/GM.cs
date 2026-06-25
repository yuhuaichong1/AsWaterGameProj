using System.Numerics;
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
    public Button SkipCheckInBtn;
    public Button SkipBankReviewBtn;

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
        {
            MoneyBtn.onClick.AddListener(OnMoneyBtnClick);
        }

        if(EnergyBtn != null && EnergyField != null)
        {
            EnergyBtn.onClick.AddListener(OnEnergyBtnClick);
        }

        if (PropBtn != null && PropDropdown != null)
        {
            PropBtn.onClick.AddListener(OnPropBtnClick);
        }

        if(SkipLevelBtn != null && SkipLevelField != null)
        {
            SkipLevelBtn.onClick.AddListener(OnSkipLevelBtnClick);
        }

        if(PassLevelBtn != null)
        {
            PassLevelBtn.onClick.AddListener(OnPassLevelBtnClick);
        }

        if(TestFunctionBtn != null)
        {
            TestFunctionBtn.onClick.AddListener(OnTestFunctionBtnClick);
        }

        if (SkipCheckInBtn != null)
        {
            SkipCheckInBtn.onClick.AddListener(OnSkipCheckInBtnClick);
        }

        if (SkipBankReviewBtn != null)
        {
            SkipBankReviewBtn.onClick.AddListener(OnSkipBankReviewBtnClick);
        }
    }

    void Update()
    {
        if (!IsGmPanelOpen())
            return;

        // GM 面板打开时可用快捷键：F1 跳过签到，F2 跳过银行审核
        if (Input.GetKeyDown(KeyCode.F1))
            GmSkipCheckInStep();
        if (Input.GetKeyDown(KeyCode.F2))
            GmSkipBankReviewStep();
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
        double money;
        if(double.TryParse(MoneyField.text, out money))
        {
            FacadePlayer.AddMoney(money);
            FacadeGamePlay.SetCurMoneyShow();
        }
    }

    private void OnEnergyBtnClick()
    {
        int energy;
        if(int.TryParse(EnergyField.text, out energy))
        {
            FacadePlayer.AddEnergy(energy);
            //FacadeGamePlay.SetCurEnergyShow(EShowEnergyType.All);
        }
    }

    private void OnPropBtnClick()
    {
        switch(PropDropdown.value) 
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
        FacadeGamePlay.IfLevelGuide();
        UIManager.Instance.OpenAsync<UILevelCompleted>(EUIType.EUILevelCompleted, UIOpenType.None, null, FacadePlayer.GetLevel());
        FacadePlayer.SetLevel(int.Parse(SkipLevelField.text));
    }

    private void OnPassLevelBtnClick()
    {
        FacadeGamePlay.IfLevelGuide();
        UIManager.Instance.OpenAsync<UILevelCompleted>(EUIType.EUILevelCompleted, UIOpenType.None, null, FacadePlayer.GetLevel());
        FacadePlayer.AddLevel(1);
    }

    private void OnSkipCheckInBtnClick()
    {
        GmSkipCheckInStep();
    }

    private void OnSkipBankReviewBtnClick()
    {
        GmSkipBankReviewStep();
    }

    /// <summary>
    /// GM：跳过签到阶段（满签 + 今日关卡完成）
    /// </summary>
    private void GmSkipCheckInStep()
    {
        if (!GameDefines.ifDebug)
            return;

        if (FacadeWithdraw.GetCurWithdrawTarget() != WithdrawTarget.CheckIn)
        {
            D.Log("[GM] 当前不是签到提现阶段，已跳过操作");
            return;
        }

        FacadeWithdraw.SetCurCheckInDay(GameDefines.CheckInDay);
        FacadeWithdraw.SetCurCheckLevel(GameDefines.CheckInLevel);
        RefreshWithdrawGmUI();
        D.Log($"[GM] 已跳过签到：{GameDefines.CheckInDay}/{GameDefines.CheckInDay} 天");
    }

    /// <summary>
    /// GM：跳过银行审核阶段（满审核天数 + 今日关卡完成）
    /// </summary>
    private void GmSkipBankReviewStep()
    {
        if (!GameDefines.ifDebug)
            return;

        if (FacadeWithdraw.GetCurWithdrawTarget() != WithdrawTarget.CheckIn)
        {
            D.Log("[GM] 当前不是签到提现阶段，已跳过操作");
            return;
        }

        if (FacadeWithdraw.GetCurCheckInDay() < GameDefines.CheckInDay)
        {
            D.Log("[GM] 签到未满，请先跳过签到或完成签到");
            return;
        }

        FacadeWithdraw.SetCurCheckInBankDay(GameDefines.CheckInBankDay);
        FacadeWithdraw.SetCurCheckLevel(GameDefines.CheckInLevel);
        RefreshWithdrawGmUI();
        D.Log($"[GM] 已跳过银行审核：{GameDefines.CheckInBankDay}/{GameDefines.CheckInBankDay} 天");
    }

    private void RefreshWithdrawGmUI()
    {
        FacadeGamePlay.SetLevelShow();
        FacadeGamePlay.SetCurMoneyShow();
    }

    private void OnTestFunctionBtnClick()
    {
        //WithdrawalRecordItem wItem = new WithdrawalRecordItem()
        //{
        //    LevelId = 0,
        //    CreatedDate = "2026/5/28",
        //    WRState = EWithRecordState.UnderReview,
        //    WRMoney = 1000.12f,
        //};
        //UIManager.Instance.OpenAsync<UIWithdrawProgress>(EUIType.EUIWithdrawalProgress, UIOpenType.None, null, wItem);

        //WithdrawalRecordItem testItem = new WithdrawalRecordItem();
        //testItem.OrderId = 999;
        //testItem.LevelId = 999;
        //testItem.CreatedDate = "9999-99-99";
        //testItem.WRState = EWithRecordState.UnderReview;
        //testItem.WRMoney = 999;
        //testItem.TargetType = WithdrawTarget.AmountOfMoney;
        //FacadeWithdraw.SetCurWithdrawTarget(WithdrawTarget.AmountOfMoney);

        //UIManager.Instance.OpenAsync<UIWithdrawProgress>(EUIType.EUIWithdrawProgress, UIOpenType.None, null, testItem);

        FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct 
        { 
            Type = ERewardType.Prop1,
            Count = 1f,
        }, null);
    }

}
