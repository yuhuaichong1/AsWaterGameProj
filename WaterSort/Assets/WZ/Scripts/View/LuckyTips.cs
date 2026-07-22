using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class LuckyTips : BaseView
    {
        private Text curLuckyCoin;
        private Button closeButton;
        private Button continuebtn;
        private bool isBound;

        private void Awake()
        {
            EnsureCanvasGroup();
            ResolveReferencesIfNeeded();
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }

        public override void InitView()
        {
            EnsureCanvasGroup();
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshLuckyWalletCoin();
            RefreshWalletBalance();
        }

        public override void Start()
        {
        }

        public override void Update()
        {
        }

        private void ResolveReferencesIfNeeded()
        {
            curLuckyCoin ??= FindChildUtility.FindChild<Text>(gameObject, "CurLuckyCoin");
            closeButton ??= FindChildUtility.FindChild<Button>(gameObject, "close");
            continuebtn ??= FindChildUtility.FindChild<Button>(gameObject, "continuebtn");
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

            closeButton?.onClick.AddListener(OnCloseClick);
            continuebtn?.onClick.AddListener(OnCloseClick);
            isBound = true;
        }

        private void RemoveEvents()
        {
            if (!isBound)
                return;

            closeButton?.onClick.RemoveListener(OnCloseClick);
            isBound = false;
        }

        private void OnCloseClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance?.CloseView(EUIType.LuckyTips);
        }

        private void RefreshLuckyWalletCoin()
        {
            if (curLuckyCoin == null || GameManagerWZ.instance == null)
                return;

            curLuckyCoin.text = FormatCoin((float)WithdrawApiService.GetCachedWalletBalance());
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
                Debug.LogError($"LuckyTips wallet balance query failed: {error}");
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
