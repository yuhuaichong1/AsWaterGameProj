using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class PayPalErrorView : BaseView
    {
        private Button reEnterButton;
        private Button confirmButton;
        private Button closeButton;
        private Text curLuckyCoinText;
        private Text email;

        private DataModel dataModel;
        private bool isBound;

        private void Awake()
        {
            EnsureCanvasGroup();
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshLuckyWalletCoin();
        }

        private void OnEnable()
        {
            RefreshLuckyWalletCoin();
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }

        public void Show(DataModel model = null)
        {
            if (model != null)
                dataModel = model;

            ShowView(model);
        }

        public void Hide()
        {
            HideView();
        }

        public void SetDataModel(DataModel model)
        {
            dataModel = model;
            RefreshLuckyWalletCoin();
        }

        public override void InitView()
        {
            EnsureCanvasGroup();
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshWalletBalance();
            RefreshLuckyWalletCoin();
        }

        public override void Start()
        {
          
        }

        public override void Update()
        {
        }

        protected override void HandleViewArgs(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] is DataModel model)
            {
                dataModel = model;
            }
        }

        private void ResolveReferencesIfNeeded()
        {
            reEnterButton = reEnterButton != null ? reEnterButton : FindChildUtility.FindChild<Button>(gameObject, "ReEnter");
            confirmButton = confirmButton != null ? confirmButton : FindChildUtility.FindChild<Button>(gameObject, "Confirm");
            email = email != null ? email : FindChildUtility.FindChild<Text>(gameObject, "email");
            closeButton = closeButton != null ? closeButton : FindChildUtility.FindChild<Button>(gameObject, "close");
            curLuckyCoinText = curLuckyCoinText != null ? curLuckyCoinText : FindChildUtility.FindChild<Text>(gameObject, "CurLuckyCoin");
            email.text= PlayerPrefs.GetString(FacadePlayerPrefExtend.AddEmail);
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void BindEvents()
        {
            if (isBound)
                return;

            reEnterButton?.onClick.AddListener(OnReEnterClick);
            confirmButton?.onClick.AddListener(OnCloseClick);
            closeButton?.onClick.AddListener(OnCloseClick);
            isBound = true;
        }

        private void RemoveEvents()
        {
            if (!isBound)
                return;

            reEnterButton?.onClick.RemoveListener(OnReEnterClick);
            confirmButton?.onClick.RemoveListener(OnCloseClick);
            closeButton?.onClick.RemoveListener(OnCloseClick);
            isBound = false;
        }

        private void OnReEnterClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            if (dataModel != null && dataModel.RetryWithdraw)
            {
                dataModel.UseProcessViewOnMethodExit = false;
                dataModel.SubmitRetryWithdrawOnMethodExit = true;
            }
            GameApp.viewManager?.Open(ViewType.WithDrawMethod, GetOrCreateDataModel());
            Hide();
        }

        private void OnCloseClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            Hide();
        }

        private DataModel GetOrCreateDataModel()
        {
            if (dataModel != null)
                return dataModel;

            dataModel = WithdrawApiService.CreateWalletWithdrawDataModel();
            return dataModel;
        }

        private void RefreshLuckyWalletCoin()
        {
            if (curLuckyCoinText == null || GameManagerWZ.instance == null)
                return;

            double amount = dataModel != null && dataModel.Money > 0d
                ? dataModel.Money
                : WithdrawApiService.GetCachedWalletBalance();
            curLuckyCoinText.text = FormatCoin((float)amount);
        }

        private void RefreshWalletBalance()
        {
            if (GameManagerWZ.instance == null)
                return;

            StartCoroutine(WithdrawApiService.QueryWalletBalance(wallet =>
            {
                RefreshLuckyWalletCoin();
            }, error =>
            {
                Debug.LogError($"钱包余额查询失败: {error}");
                RefreshLuckyWalletCoin();
            }));
        }

        private string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            return $"{GetCurrencyMark()}{FormatPlainCoin(coin)}";
        }

        private string FormatPlainCoin(float coin)
        {
            int decimals = GameManagerWZ.instance != null ? Mathf.Max(GameManagerWZ.instance.decimals, 0) : 2;
            string formatted = coin.ToString($"F{decimals}");
            return formatted.TrimEnd('0').TrimEnd('.');
        }

        private string GetCurrencyMark()
        {
            return GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
        }
    }
}
