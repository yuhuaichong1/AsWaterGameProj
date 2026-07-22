namespace WZSDK
{
    using System;
    
    [Serializable]
    public class HistoryModel
    {
        private string id;
        private string dateTime;
        private double money;
        private int stage;
        private int status;//1 已提现 2未提现
    
        public string DateTime
        {
            get => dateTime;
            set => dateTime = value;
        }
    
        public double Money
        {
            get => money;
            set => money = value;
        }
    
        public int Status
        {
            get => status;
            set => status = value;
        }
    
        public string ID
        {
            get => id;
            set => id = value;
        }
    
        public int Stage
        {
            get => stage;
            set => stage = value;
        }
    }
}
