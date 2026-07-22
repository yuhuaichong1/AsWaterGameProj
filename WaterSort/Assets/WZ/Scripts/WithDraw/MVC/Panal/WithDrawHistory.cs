using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class WithDrawHistory : BaseViewWithDraw
    {
        private const bool ForcePayPalErrorOnOrderClick = false;
        private const string DefaultHistoryItemPrefabPath = "Prefab/historyItem";
        private const string WalletHistoryItemPrefabPath = "Prefab/historyItemBlue";
        private const string DefaultPayPalAccountErrorTip = "当前PayPal收款账号不存在，请重新填写正确的PayPal邮箱";

        private Text moneyText;
        private GameObject content;
        private readonly List<HistoryItem> historyItems = new List<HistoryItem>();
        private bool isQueryingOrderStatus;
        private bool isRefreshingOrders;

        protected override void OnAwake()
        {
            moneyText = Find<Text>("Panel/PanelA/Num");
            content = Find("Panel/Scroll View/Viewport/Content");
            Find<Button>("Panel/bg/close").onClick.AddListener(OnCloseBtnClick);
        }

        protected override void HandleViewArgs(object[] args)
        {
            PopulateOrders(WithdrawApiService.GetCachedOrders());
            StartCoroutine(RefreshOrdersCoroutine());
        }

        private IEnumerator RefreshOrdersCoroutine()
        {
            isRefreshingOrders = true;
            ApplyHistoryItemInteractionState();

            bool requestCompleted = false;
            List<WithdrawOrderData> latestOrders = null;

            yield return WithdrawApiService.QueryWithdrawOrders(orders =>
            {
                requestCompleted = true;
                latestOrders = orders ?? new List<WithdrawOrderData>();
                LogFetchedOrderStatuses(latestOrders);
                PopulateOrders(latestOrders);
            }, error =>
            {
                requestCompleted = true;
                Debug.LogError($"提现订单列表查询失败: {error}");
            });

            if (!requestCompleted)
                Debug.LogWarning("提现订单列表查询未完成，继续使用当前缓存状态。");

            if (latestOrders != null && latestOrders.Count > 0)
                yield return RefreshEachOrderStatusCoroutine(latestOrders);

            isRefreshingOrders = false;
            ApplyHistoryItemInteractionState();
        }

        private IEnumerator RefreshEachOrderStatusCoroutine(List<WithdrawOrderData> orders)
        {
            for (int i = 0; i < orders.Count; i++)
            {
                WithdrawOrderData order = orders[i];
                if (order == null
                    || order.ClientQueuedPlaceholder
                    || string.IsNullOrEmpty(order.OrderNo)
                    || order.OrderStatus == 1)
                    continue;

                bool requestCompleted = false;
                string currentOrderNo = order.OrderNo;

                yield return WithdrawApiService.QueryWithdrawOrder(currentOrderNo, refreshedOrder =>
                {
                    requestCompleted = true;
                    LogRefreshedOrderRealStatus(refreshedOrder ?? order);
                    PopulateOrders(WithdrawApiService.GetCachedOrders());
                    LogOfficialStatusIfDisplayedAsReview(refreshedOrder ?? order);
                }, error =>
                {
                    requestCompleted = true;
                    Debug.LogError($"提现订单状态查询失败: orderNo={currentOrderNo}, error={error}");
                });

                if (!requestCompleted)
                    Debug.LogWarning($"提现订单状态查询未完成: orderNo={currentOrderNo}");
            }
        }

        private void PopulateOrders(List<WithdrawOrderData> orders)
        {
            if (content == null)
                return;

            historyItems.Clear();
            for (int i = content.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(content.transform.GetChild(i).gameObject);
            }

            List<WithdrawOrderData> safeOrders = SortOrdersForDisplay(orders);
            double total = safeOrders
                .Where(order => order != null && order.OrderStatus == 1)
                .Sum(order => order.GetAmountValue());

            if (moneyText != null)
                moneyText.text = GameManagerWZ.instance.mark + total.ToString("F2");

            foreach (WithdrawOrderData order in safeOrders)
            {
                GameObject load = LoadHistoryItemPrefab(order);
                if (load == null)
                    continue;

                GameObject obj = Instantiate(load, content.transform);
                HistoryItem historyItem = obj.GetComponent<HistoryItem>();
                if (historyItem == null)
                    continue;

                historyItem.init(order, OnOrderClick);
                historyItem.SetInteractionBlocked(isQueryingOrderStatus || isRefreshingOrders);
                historyItems.Add(historyItem);
            }
        }

        private GameObject LoadHistoryItemPrefab(WithdrawOrderData order)
        {
            string prefabPath = ResolveHistoryItemPrefabPath(order);
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null)
                return prefab;

            if (!string.Equals(prefabPath, DefaultHistoryItemPrefabPath, StringComparison.Ordinal))
            {
                Debug.LogWarning($"WithDrawHistory history item prefab missing: {prefabPath}, fallback={DefaultHistoryItemPrefabPath}");
                return Resources.Load<GameObject>(DefaultHistoryItemPrefabPath);
            }

            Debug.LogWarning($"WithDrawHistory history item prefab missing: {prefabPath}");
            return null;
        }

        private string ResolveHistoryItemPrefabPath(WithdrawOrderData order)
        {
            return order != null && order.IsWalletWithdraw
                ? WalletHistoryItemPrefabPath
                : DefaultHistoryItemPrefabPath;
        }

        private void OnOrderClick(WithdrawOrderData order)
        {
            if (order == null || isQueryingOrderStatus || isRefreshingOrders)
                return;

            if (!GameDefines.EnableWithdrawHistoryAllStatusClick && !order.RequiresReceiverInput)
                return;

            isQueryingOrderStatus = true;
            ApplyHistoryItemInteractionState();
            Debug.Log(GetOrderStatusLog(order));

            if (ForcePayPalErrorOnOrderClick)
            {
                OpenForcedPayPalErrorView(order);
                FinishOrderQueryInteraction();
                return;
            }

            HandleOrderAction(order);
            FinishOrderQueryInteraction();
        }

        private string GetOrderStatusLog(WithdrawOrderData order)
        {
            if (order == null)
                return "Withdraw order clicked: null";

            if (ShouldDisplayReviewStatus(order))
            {
                return $"Withdraw order clicked: orderNo={order.OrderNo}, type={order.WithdrawType}, status={order.OrderStatus}, officialStatus={order.GetStatusText()}, displayStatus={GetDisplayStatusText(order)}";
            }

            return $"Withdraw order clicked: orderNo={order.OrderNo}, type={order.WithdrawType}, status={order.OrderStatus}, text={order.GetStatusText()}";
        }

        private void OpenForcedPayPalErrorView(WithdrawOrderData order)
        {
            DataModel retryData = WithdrawApiService.CreateRetryWithdrawDataModel(order);
            if (retryData == null)
                return;

            retryData.RetryWithdraw = true;
            retryData.PayPalAccountExists = 0;
            retryData.PayPalAccountTip = string.IsNullOrEmpty(retryData.PayPalAccountTip)
                ? DefaultPayPalAccountErrorTip
                : retryData.PayPalAccountTip;

            UIManager.instance?.ShowView(EUIType.PayPalErrorView, retryData);
        }

        private void HandleOrderAction(WithdrawOrderData order)
        {
            if (order == null)
                return;

            LogOfficialStatusIfDisplayedAsReview(order);

            if (order.ClientQueuedPlaceholder)
            {
                WithdrawApiService.LogPaymentNotice("WithDrawHistory", order.GetStatusText(), LogType.Log);
                return;
            }

            if (order.OrderStatus == 1 || order.IsPending || order.OrderStatus == 5)
            {
                WithdrawApiService.LogPaymentNotice("WithDrawHistory", order.GetStatusText(), LogType.Log);
                return;
            }

            if (!order.CanClientRetry)
            {
                WithdrawApiService.LogPaymentNotice("WithDrawHistory", order.GetStatusText(), LogType.Log);
                return;
            }

            if (order.RequiresReceiverInput)
            {
                DataModel retryData = WithdrawApiService.CreateRetryWithdrawDataModel(order);
                if (retryData != null)
                {
                    retryData.PayPalAccountExists = 0;
                    retryData.PayPalAccountTip = string.IsNullOrEmpty(retryData.PayPalAccountTip)
                        ? DefaultPayPalAccountErrorTip
                        : retryData.PayPalAccountTip;
                }
                UIManager.instance?.ShowView(EUIType.PayPalErrorView, retryData);
                return;
            }

            WithdrawApiService.LogPaymentNotice("WithDrawHistory", order.GetStatusText(), LogType.Warning);
        }

        public void OnCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);
        }

        private void FinishOrderQueryInteraction()
        {
            isQueryingOrderStatus = false;
            ApplyHistoryItemInteractionState();
        }

        private void ApplyHistoryItemInteractionState()
        {
            for (int i = 0; i < historyItems.Count; i++)
            {
                if (historyItems[i] != null)
                    historyItems[i].SetInteractionBlocked(isQueryingOrderStatus || isRefreshingOrders);
            }
        }

        private void LogFetchedOrderStatuses(List<WithdrawOrderData> orders)
        {
            List<WithdrawOrderData> safeOrders = SortOrdersForDisplay(orders);
            int serverOrderCount = safeOrders.Count(order => order != null && !order.ClientQueuedPlaceholder);
            if (serverOrderCount == 0)
            {
                WithdrawApiService.LogPaymentNotice("WithDrawHistory", "打开提现记录时，服务器未返回任何订单。", LogType.Log);
            }

            for (int i = 0; i < safeOrders.Count; i++)
            {
                WithdrawApiService.LogPaymentNotice("WithDrawHistory",
                    GetFetchedOrderStatusLog(safeOrders[i], i + 1, safeOrders.Count),
                    LogType.Log);
            }
        }

        private void LogRefreshedOrderRealStatus(WithdrawOrderData order)
        {
            if (order == null)
            {
                WithdrawApiService.LogPaymentNotice("WithDrawHistory", "逐单查询订单真实状态时返回了空订单。", LogType.Warning);
                return;
            }

            WithdrawApiService.LogPaymentNotice("WithDrawHistory",
                $"订单真实状态: orderNo={order.OrderNo}, type={order.WithdrawType}, status={order.OrderStatus}, officialStatus={order.GetStatusText()}, displayStatus={GetDisplayStatusText(order)}",
                LogType.Log);
        }

        private string GetFetchedOrderStatusLog(WithdrawOrderData order, int index, int totalCount)
        {
            if (order == null)
                return $"订单列表状态[{index}/{totalCount}]: null";

            string source = order.ClientQueuedPlaceholder ? "localPlaceholder" : "server";
            return $"订单列表状态[{index}/{totalCount}]: source={source}, orderNo={order.OrderNo}, type={order.WithdrawType}, status={order.OrderStatus}, officialStatus={order.GetStatusText()}, displayStatus={GetDisplayStatusText(order)}";
        }

        private void LogOfficialStatusIfDisplayedAsReview(WithdrawOrderData order)
        {
            if (!ShouldDisplayReviewStatus(order))
                return;

            WithdrawApiService.LogPaymentNotice("WithDrawHistory",
                $"正式状态: {order.GetStatusText()} (orderStatus={order.OrderStatus}), 列表显示: {GetDisplayStatusText(order)}",
                LogType.Log);
        }

        private bool ShouldDisplayReviewStatus(WithdrawOrderData order)
        {
            return order != null && (order.OrderStatus == 4 || order.OrderStatus == 5);
        }

        private string GetDisplayStatusText(WithdrawOrderData order)
        {
            if (order == null)
                return string.Empty;

            if (!ShouldDisplayReviewStatus(order))
                return order.GetStatusText();

            return GetLocalizedText("3030", "审核中");
        }

        private string GetLocalizedText(string key, string fallback)
        {
            if (LocalizationManager.Instance == null)
                return fallback;

            string text = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private List<WithdrawOrderData> SortOrdersForDisplay(List<WithdrawOrderData> orders)
        {
            return (orders ?? new List<WithdrawOrderData>())
                .Where(order => order != null)
                .OrderBy(ResolveOrderDisplayTime)
                .ThenBy(order => order.GetBestTimeText())
                .ThenBy(order => order.OrderNo)
                .ToList();
        }

        private DateTimeOffset ResolveOrderDisplayTime(WithdrawOrderData order)
        {
            if (order == null)
                return DateTimeOffset.MinValue;

            if (order.TryGetSortAnchorTime(out DateTimeOffset sortTime))
                return sortTime;

            return DateTimeOffset.MinValue;
        }
    }
}
