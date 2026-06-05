using SuperScrollView;
using System.Collections.Generic;

namespace XrCode
{

    public partial class UIWithdrawRecords : BaseUI
    {
        private bool ifDetial;
        private bool ifDetialSuccess;

        List<WithdrawalRecordItem> withdrawalRecordItems;

        protected override void OnAwake()
        {
            withdrawalRecordItems = FacadeWithdraw.GetWithdrawalRecordItems();
            mOrderScrollView.InitGridView(withdrawalRecordItems.Count, OrdersCallBack);
        }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);

            mCurMoneyText.text = FacadePayType.RegionalChange(FacadeWithdraw.GetTotalRecordMoney());
            int wriLength = withdrawalRecordItems.Count;
            mOrderScrollView.SetListItemCount(wriLength);
            mOrderScrollView.RefreshAllShownItem();
        }

        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawRecords);
            });
        }

        private LoopGridViewItem OrdersCallBack(LoopGridView cell, int index, int row, int column)
        {
            LoopGridViewItem item = mOrderScrollView.NewListViewItem("OrderItem");
            OrderItem orderItem = item.GetComponent<OrderItem>();
            orderItem.SetInfo(withdrawalRecordItems[index]);
            return item;
        }

        protected override void OnDisable()
        {
        
        }
        protected override void OnDispose()
        {
        
        }
    }
}