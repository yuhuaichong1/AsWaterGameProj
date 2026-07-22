namespace WZSDK
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public static class FacadePayTypeExtend
    {
        //public static Func<List<PayNode>> GetPayNodeList;                   //获取支付类型
        public static Func<float, string> RegionalChangeHandle;             //将值以汇率的方式显示
        public static Func<int> GetNANPHandle;                              //获取国际长途电话区号
        public static Func<string> GetCountryCodeHandle;                    //获取国家码
        public static Func<string> GetLanguageHandle;                       //获取语言
        public static Func<Sprite> GetSingleCoin;                           //获取单个钱图片
        public static Func<Sprite> GetMultCoinCoinAct;                           //获取钱堆叠图片
        public static Func<Sprite> GetSingleGem;                           //获取单个钻石图片
        public static Func<Sprite> GetMulteGem;                           //获取一堆钻石图片
        public static Func<Sprite> GetMultCoin;                             //获取一堆钱图片
    
    }
}
