namespace WZSDK
{
    public class DataModel
    {
        public const int SpecialNone = 0;
        public const int SpecialRewardAlreadyGranted = 1;
        public const int SpecialLuckyWalletWithdraw = 2;
        public const int SpecialWithdrawMissionBindEmail = 3;

        private int level;
        private int num;
        private double money;
        private int index;
        private string name;
        private string phone;
        private string email;
        private int type;// 1 3个 2 5个
        private string historyId;
        private int closeType;// 1正常关 2跳到页面
        private int special;//特殊处理
        private string orderNo;
        private string receiver;
        private string withdrawType;
        private int orderStatus;
        private int walletFrozen;
        private int? payPalAccountExists;
        private string payPalAccountTip;
        private bool retryWithdraw;
        private string queuedOrderNo;
        private bool submitOnOpen;
        private bool useProcessViewOnMethodExit;
        private bool submitRetryWithdrawOnMethodExit;
        private bool closeWithoutAdvance;
    
        
    
        public DataModel()
        {
        }
    
        public int Level
        {
            get => level;
            set => level = value;
        }
    
        public int Num
        {
            get => num;
            set => num = value;
        }
    
        public double Money
        {
            get => money;
            set => money = value;
        }
    
        public int Index
        {
            get => index;
            set => index = value;
        }
    
        public string Name
        {
            get => name;
            set => name = value;
        }
    
        public string Phone
        {
            get => phone;
            set => phone = value;
        }
    
        public string Email
        {
            get => email;
            set => email = value;
        }
    
        public int Type
        {
            get => type;
            set => type = value;
        }
    
        public string HistoryId
        {
            get => historyId;
            set => historyId = value;
        }
    
        public int CloseType
        {
            get => closeType;
            set => closeType = value;
        }  
        public int Special
        {
            get => special;
            set => special = value;
        }

        public string OrderNo
        {
            get => orderNo;
            set => orderNo = value;
        }

        public string Receiver
        {
            get => receiver;
            set => receiver = value;
        }

        public string WithdrawType
        {
            get => withdrawType;
            set => withdrawType = value;
        }

        public int OrderStatus
        {
            get => orderStatus;
            set => orderStatus = value;
        }

        public int WalletFrozen
        {
            get => walletFrozen;
            set => walletFrozen = value;
        }

        public int? PayPalAccountExists
        {
            get => payPalAccountExists;
            set => payPalAccountExists = value;
        }

        public string PayPalAccountTip
        {
            get => payPalAccountTip;
            set => payPalAccountTip = value;
        }

        public bool RetryWithdraw
        {
            get => retryWithdraw;
            set => retryWithdraw = value;
        }

        public string QueuedOrderNo
        {
            get => queuedOrderNo;
            set => queuedOrderNo = value;
        }

        public bool SubmitOnOpen
        {
            get => submitOnOpen;
            set => submitOnOpen = value;
        }

        public bool UseProcessViewOnMethodExit
        {
            get => useProcessViewOnMethodExit;
            set => useProcessViewOnMethodExit = value;
        }

        public bool SubmitRetryWithdrawOnMethodExit
        {
            get => submitRetryWithdrawOnMethodExit;
            set => submitRetryWithdrawOnMethodExit = value;
        }

        public bool CloseWithoutAdvance
        {
            get => closeWithoutAdvance;
            set => closeWithoutAdvance = value;
        }
    }
}
