
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawGoal2 : BaseUI
    {
        private readonly List<WOTypeItem> channelTabs = new List<WOTypeItem>();
        private readonly List<WOListItem> listItems = new List<WOListItem>();
        private List<PayNode> payItems;
        private int curChannelIndex;

        protected override void OnAwake()
        {
            mWOTypeItem.gameObject.SetActive(false);
            mWOListItem.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            FacadeEvent.AddEventListener(PayoutEventTypes.ENTRY_UPDATED, OnPayoutUpdated);
            ReloadRegionPayChannels();
            RefreshHeader();
            BuildChannelTabs();
            RefreshTierList();
            ShowAnim(mPlane);

            FacadeWithdraw.RefreshHeader += RefreshHeader;
        }

        protected override void OnDisable()
        {
            FacadeEvent.RemoveEventListener(PayoutEventTypes.ENTRY_UPDATED, OnPayoutUpdated);
        }

        /// <summary>
        /// 按 PayRegion 表当前国家支持的 Channels 刷新渠道列表（由 PayTypeModule 解析）。
        /// </summary>
        private void ReloadRegionPayChannels()
        {
            payItems = FacadePayType.GetPayItems();
            if (payItems == null)
                payItems = new List<PayNode>();
        }

        private void OnPayoutUpdated(EventStruct evt)
        {
            RefreshHeader();
            RefreshTierList();
        }

        private void RefreshHeader()
        {
            mTBContent.text = FacadePayType.RegionalChange(FacadePlayer.GetMoney());
            if (payItems == null || payItems.Count == 0) return;

            var payType = payItems[Mathf.Clamp(curChannelIndex, 0, payItems.Count - 1)].payType;
            PayNode node = payItems.Find(p => p.payType == payType) ?? payItems[curChannelIndex];
            mCurWIcon.sprite = node.picture;
            mCurWInfo.text = FacadeWithdraw.GetWPhoneOrEmail2(node.payType);
        }

        private void BuildChannelTabs()
        {
            foreach (var tab in channelTabs)
            {
                if (tab == null) continue;
                tab.Toggle.onValueChanged.RemoveAllListeners();
                GameObject.Destroy(tab.gameObject);
            }
            channelTabs.Clear();

            if (payItems == null || payItems.Count == 0) return;

            for (int i = 0; i < payItems.Count; i++)
            {
                var item = GameObject.Instantiate(mWOTypeItem, mWOTContent);
                item.gameObject.SetActive(true);
                item.Icon.sprite = payItems[i].picture;
                int index = i;
                item.Toggle.onValueChanged.AddListener(isOn =>
                {
                    if (!isOn) return;
                    curChannelIndex = index;
                    FacadeWithdraw.SetPayType(payItems[index].payType);
                    RefreshHeader();
                    RefreshTierList();
                });
                channelTabs.Add(item);
            }

            curChannelIndex = 0;
            channelTabs[0].Toggle.SetIsOnWithoutNotify(true);
            FacadeWithdraw.SetPayType(payItems[0].payType);
        }

        private void RefreshTierList()
        {
            foreach (var item in listItems)
            {
                if (item != null) GameObject.Destroy(item.gameObject);
            }
            listItems.Clear();

            if (payItems == null || payItems.Count == 0) return;

            curChannelIndex = Mathf.Clamp(curChannelIndex, 0, payItems.Count - 1);
            var channel = payItems[curChannelIndex].payType;
            var tiers = FacadePayout.GetTierList();
            foreach (var tier in tiers)
            {
                var item = GameObject.Instantiate(mWOListItem, mWOLContent);
                item.gameObject.SetActive(true);
                var key = new PayoutEntryKey(channel, tier.Sn);
                item.Init(key, payItems[curChannelIndex].picture, tier.TargetAmount, RefreshTierList);
                listItems.Add(item);
            }
        }

        private void OnExitBtnClickHandle()
        {
            UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal2);
        }

        private void OnSetCurWInfoBtnClickHandle()
        {
            UIManager.Instance.OpenAsync<UIWithdrawAccount>(EUIType.EUIWithdrawAccount, UIOpenType.None, null, curChannelIndex);
        }

        protected override void OnDispose()
        {
            FacadeWithdraw.RefreshHeader += RefreshHeader;
        }
    }
}
