using System;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class HistoryItem : MonoBehaviour
    {


        private Text dateTxt;
        private Text moneyTxt;
        private Text dateTitle;
        private Text statusTitle;

        private Button btn;
        private WithdrawOrderData orderData;
        private Action<WithdrawOrderData> clickCallback;
        private bool baseInteractable = true;
        private bool interactionBlocked;

        private void Awake()
        {
            dateTxt = FindText("ContentA");
            moneyTxt = FindText("ContentB");
            dateTitle = FindText("titleA");
            statusTitle = FindText("titleB");
            btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            if (btn != null)
                btn.onClick.RemoveListener(OnButtonClick);
        }

        public void init(WithdrawOrderData order, Action<WithdrawOrderData> onClick)
        {
            orderData = order;
            clickCallback = onClick;

            if (dateTxt != null)
                dateTxt.text = FormatDate(order != null ? order.GetBestTimeText() : string.Empty);

            if (moneyTxt != null)
                moneyTxt.text = GameManagerWZ.instance.mark + (order != null ? order.GetAmountValue().ToString("F2") : "0.00");

            if (dateTitle != null)
                dateTitle.text = order != null ? order.WithdrawType : string.Empty;

            if (statusTitle != null)
                statusTitle.text = GetLocalizedStatusText(order);

            ApplyOrderStyle(order);
        }

        public void SetInteractionBlocked(bool blocked)
        {
            interactionBlocked = blocked;
            ApplyInteractableState();
        }

        private Text FindText(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private string GetLocalizedStatusText(WithdrawOrderData order)
        {
            if (order == null)
                return string.Empty;

            string key = GetStatusTextKey(order);
            if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
            {
                string text = LocalizationManager.Instance.GetText(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                    return text;
            }

            return order.GetStatusText();
        }

        private string GetStatusTextKey(WithdrawOrderData order)
        {
            if (order == null)
                return string.Empty;

            if (order.ClientQueuedPlaceholder && !string.IsNullOrEmpty(order.LocalStatusText))
            {
                if (ContainsStatusKeyword(order.LocalStatusText, "排队", "queue"))
                    return "3029";

                if (ContainsStatusKeyword(order.LocalStatusText, "审核", "review"))
                    return "3030";
            }

            switch (order.OrderStatus)
            {
                case 0: return "3030";
                case 1: return "3032";
                case 2: return "3090";
                case 3: return "3031";
                case 4: return "3030";
                case 5: return "3030";
                default: return "3092";
            }
        }

        private bool ContainsStatusKeyword(string source, string chineseKeyword, string englishKeyword)
        {
            if (string.IsNullOrEmpty(source))
                return false;

            return (!string.IsNullOrEmpty(chineseKeyword) && source.Contains(chineseKeyword))
                || (!string.IsNullOrEmpty(englishKeyword) && source.IndexOf(englishKeyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private string FormatDate(string rawDate)
        {
            if (string.IsNullOrEmpty(rawDate))
                return string.Empty;

            if (DateTime.TryParse(rawDate, out DateTime parsed))
                return parsed.ToString("MM/dd/yyyy HH:mm");

            return rawDate;
        }

        public void OnButtonClick()
        {
            if (orderData == null)
                return;

            clickCallback?.Invoke(orderData);
        }

        private void ApplyOrderStyle(WithdrawOrderData order)
        {
           

            baseInteractable = ResolveBaseInteractable(order);
            ApplyInteractableState();
        }

        private bool ResolveBaseInteractable(WithdrawOrderData order)
        {
            if (order == null)
                return false;

            if (GameDefines.EnableWithdrawHistoryAllStatusClick)
                return true;

            return order.RequiresReceiverInput;
        }

        private void ApplyInteractableState()
        {
            if (btn != null)
                btn.interactable = baseInteractable && !interactionBlocked;
        }

      
    }
}
