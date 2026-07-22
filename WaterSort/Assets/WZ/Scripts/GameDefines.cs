
/* 游戏定义脚本
 * 游戏中常量、枚举等内容请在这里统一定义
 */
namespace WZSDK
{

    #region 常量

    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class GameDefines
    {
        #region 打包相关
        public static int appIndex = 48;
        public static string URL = "http://www.game175.fun/xgame?appIndex=48"; //进入游戏请求
        public static string PushApiBaseUrl = "http://www.game175.fun";        // 推送接口线上
        public const string  FirebasePushTriggerUrl = "http://www.game175.fun/fireBase";
        public static string WithdrawApiBaseUrl = PushApiBaseUrl;             // 提现接口域名

     
   
        //public static string PushApiBaseUrl = "http://192.168.105.64:8020";                     // 推送接口测试

      
        public static bool EnableWithdrawMissionServerRequest = true;                    // 是否允许提现任务界面向服务器提交提现请求
        public static bool EnableWithdrawMissionFirstAdTestButton = false;                // 是否显示提现任务界面的首提测试按钮
        public static bool EnableWithdrawHistoryAllStatusClick = false;                   // 提现历史页是否允许所有状态点击，false 时仅 PayPal 账号不存在可点击
        public static float WheelReward = 0.1f;                                                           // LuckySpin 小钱包首转奖励

   
        public static string HeartbeatApiPath = "/client/push/heartbeat";
        public static string PushTokenApiPath = "/client/push/token";
        public static string PushEventApiPath = "/client/push/event";
        public static string PushOpenApiPath = "/client/push/open";
        public static string AfInitApiPath = "/user/af";
        public static string AfAdCountApiPath = "/user/af/ad/count";
        public static string AfInitStatusName = "NON_ORGANIC";



        public static string WalletBalanceApiPath = "/client/wallet/balance";
        public static string WalletIncomeApiPath = "/client/wallet/income";
        public static string LuckyRewardConfigApiPath = "/user/af/lucky/amount";               //todo 幸运钱包奖励金额接口，未配置时客户端回退本地值
        public static string WalletWithdrawApiPath = "/paypal/client/withdraw";
        public static string WalletWithdrawOrdersApiPath = "/paypal/client/withdraw/orders";
        public static string WalletWithdrawOrderApiPath = "/paypal/client/withdraw/order";


        public static float HeartbeatIntervalSeconds = 60f;                                                            // 心跳发送间隔（秒）
        public static bool ifIAA = true;                                                                       //是否为IAA模式



        #endregion

        public static string BuildPushApiUrl(string path)
        {
            return BuildApiUrl(PushApiBaseUrl, path);
        }

        public static string BuildWithdrawApiUrl(string path)
        {
            return BuildApiUrl(WithdrawApiBaseUrl, path);
        }

        public static string BuildApiUrl(string baseUrl, string path)
        {
            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            try
            {
                UriBuilder builder = new UriBuilder(baseUrl)
                {
                    Path = path.TrimStart('/'),
                    Query = string.Empty
                };
                return builder.Uri.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"API地址生成失败: {e.Message}");
                return string.Empty;
            }
        }

        public static string BuildHeartbeatUrl(string afDeviceId)
        {
            if (string.IsNullOrEmpty(afDeviceId) || string.IsNullOrEmpty(HeartbeatApiPath))
            {
                return string.Empty;
            }

            string heartbeatBaseUrl = BuildPushApiUrl(HeartbeatApiPath);
            if (string.IsNullOrEmpty(heartbeatBaseUrl))
            {
                return string.Empty;
            }

            string escapedAfDeviceId = Uri.EscapeDataString(afDeviceId);
            if (heartbeatBaseUrl.IndexOf("afDeviceId=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"{heartbeatBaseUrl}{escapedAfDeviceId}";
            }

            string separator = heartbeatBaseUrl.Contains("?") ? "&" : "?";
            return $"{heartbeatBaseUrl}{separator}afDeviceId={escapedAfDeviceId}";
        }






        public static float Elimination_Money
        {
            get
            {
                if (GameManagerWZ.instance != null)
                {
                    return GameManagerWZ.instance.getResourceNum(1, 2, 0);
                }
                return 5f;
            }
        }

        public static float IAA_Elimination_Money
        {
            get
            {
                if (GameManagerWZ.instance != null)
                {
                    return GameManagerWZ.instance.getResourceNum(1, 2, 0);
                }
                return 1f;
            }
        }


        public static float Elimination_Gem_Coefficient = 1f;                                             //单次消除钻石;
        public static int Elimination_FlyMoneyCount = 8;                                                        //单次消除后飞行钱特效的数量

        #endregion

        public static int LuckyWalletUnlockLevel = 8;                                                           // 幸运钱包解锁和幸运进度起始关卡（当前关卡大于等于该值时解锁）
        public static readonly List<int> ServerLuckyRewardLevels = new List<int>();                             // 服务器下发的幸运关卡列表，未下发时回退CSV
        public static float LuckyChestPlaceholderCoin = 0.00f;                                                    // 幸运钱包金额占位值（服务器下发前使用）
        public static float AddCoin = 0.1f;                                                    // 幸运钱包金额占位值（服务器下发前使用）
        #region 特效相关
        public static float GRE_StayTime = 1f;                                                                  //获取奖励特效持续时间（同时也是之后的飞物体特效的延迟时间）
        public static int GRE_FlyMoneyCount = 8;                                                                //奖励特效执行飞行钱特效时飞钱的数量
        public static float LTE_MoveTime = 0.5f;                                                                //关卡目标特效移入时间
        public static float LTE_StayTime = 2f;                                                                  //关卡目标特效持续时间
        public static float LTE_GoAwayTime = 0.5f;                                                              //关卡目标特效移出时间
        public static float CE_MoveTime = 0.2f;                                                                 //祝贺特效移入时间
        public static float CE_StayTime = 2f;                                                                   //祝贺特效持续时间
        public static Vector2 CE_Content_attemptTimes = new Vector2(1, 2);                                      //祝贺特效内容随机尝试次数
                                                                                                                //public static string nameString = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";   //随机名称数组
        public static string nameString = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";                                         //随机名称数组
        public static float FlyProp_MoveTime = 0.5f;                                                            //飞行道具持续时间
        public static float FlyMoney_MoveTime = 0.5f;                                                           //飞行钱持续时间
        public static float FlyMoney_DelayTime = 0.5f;                                                          //飞行钱延迟播放时间
        public static float FlyMoney_IntervalTime = 0.1f;                                                       //飞行钱间隔时间
        public static float FlyMoney_RandomSpawnDist = 100f;                                                    //飞行钱随机生成位置的区间
        public static float FlyMoneyTip_DelayTime = 1f;                                                         //飞行钱提示延迟播放时间
        public static float FlyMoneyTip_MoveTime = 2f;                                                          //飞行钱提示持续时间
        public static float FlyMoneyTip_MoveDist = 60f;                                                         //飞行钱提示移动距离
        public static float DifficultyUp_StayTime = 1;                                                          //难度提升特效持续时间
        public static Vector2 TargetArriveTime = new Vector2(1, 2);                                             //关卡目标特效随机时间
        #endregion

  

        #region UI打开、关闭动画时间
        public static float ShowAnimTime = 0.25f;                                                               //UI动画打开持续时间
        public static float HideAnimTime = 0.25f;                                                               //UI动画关闭持续时间
        #endregion




        #region 各种路径



        #region 奖励特效小图标路径

        public static string ERIAAMoneyIconPath = "UI/LuckySpinIcons/icon_qianbi_IAA";                      //一叠硬币
        public static string ERMultCoinIconPath = "UI/Money/Multiple/coin_big_us";                          //成功页/奖励项钱堆
        public static string ERFlyMoneyIconPath = "UI/LuckySpinView/icon_qianbi";                           //飞币小图标
        public static string ERAddSpaceIconPath = "UI/FuncIcon/Bottle";                         //添加瓶子
        public static string ERClearIconPath = "UI/FuncIcon/icon_mofab";                               //清除道具
        public static string ERHammerIconPath = "UI/FuncIcon/icon_shuaxin";                             //回退道具
        public static string ERAddPromptIconPath = "UINew/dengpao";
        public static string ERAddGuidIconPath = "UINew/chizi";
        public static string Default_SingleGem = "UI/Money/gem";                                            //默认钻石路径
        public static string Default_SingDuileGem = "UI/Money/zuanshi_image";                                            //默认钻石堆路径

        public static string LSIAAMoneyIconPath = "UI/LuckySpinIcons/icon_qianbi_IAA";                      //一叠硬币
        public static string LSLuckyWalletIconPath = "UI/PayType/Icon/paypal_s";                                 // 幸运钱包图标
        #endregion



        #endregion

        public static Dictionary<int, float> LevelTimerConfig = new Dictionary<int, float>();//通过接受服务器的时间决定奖励面板弹出时间
        public static Dictionary<int, int> LevelMapConfig = new Dictionary<int, int>();
        public static int LuckySpin_Elimination_Interval = 40;  // 每40次消除触发一次抽奖
        public static float TimerReward_Interval = 30;  // 定时奖励间隔（秒）（开关关闭时使用）
        public static bool EnableLuckyRewardOnPourComplete = false;// true：在倒水完成后检查并触发 Lucky 广告；false：不触发倒水计时类 Lucky 广告

        public static void ApplyServerLuckyRewardLevels(IEnumerable<int> levels)
        {
            ServerLuckyRewardLevels.Clear();
            if (levels == null)
                return;

            HashSet<int> seenLevels = new HashSet<int>();
            foreach (int level in levels)
            {
                if (level <= 0 || !seenLevels.Add(level))
                    continue;

                ServerLuckyRewardLevels.Add(level);
            }

            ServerLuckyRewardLevels.Sort();
        }

        #region 幸运转盘相关
        public static List<float> LS_Angles = new List<float>() { 33, 95, 153, 210, 266, 327 };                   //LuckySpin转盘对应角度
        public static List<float> BonusWheel_Angles = new List<float>() { 180, 135, 90, 45,0,-45,90,-135};                    //BonusWheel独立转盘角度，仅控制表现
        public static int LS_rotateCount = 6;                                                                   //旋转圈数
        public static float LS_rotateTime = 4f;                                                                 //转完所需时间

        #endregion


        public static int EXP_Single = 2;                                                                       //单次消除获取的经验

        public static int SmallLevelGroupStart = 9;
        public static int SmallLevelGroupEnd = 17;
        public static int CloseInt = 3;

      
    }



    #region 枚举



    /// <summary>
    /// UI类型枚举  和ui表配置一一对应
    /// </summary>
    public enum EUIType
    {
        ENone = 0,
        GameView,
        Tutorial,
        SettingsView,
        ProfileView,
        FinishView,
        WarningView,
        UserLevelView,
        ReStartView,
        ChallangeSuccessView,
        UIEffectView,
        LuckySpinView,
        PropView,
        RewardView,
        NewGamePlay,
        LoadingView,
        NoticeView,
        GamePlayerView,
        WithdrawView,
        WelcomeView,
        FailView,
        SlotView,
        WithdrawMissionView,
        TargetWithdrawView,
        GameBalancView,
        LuckyWalletView,
        LuckySuccessView,
        PayPalErrorView,
        BloatView,
        BloatProgressView,
        BonusWheelView,
        LuckyTips,
        WithdrawFeedbackView,
        ProgressView,
    }

    public enum AdDisplayFailed
    {
        InitFailed,
        AdLoadFailed
    }
    /// <summary>
    /// 广告来源
    /// </summary>
    public enum EAdSource
    {
        Prop,//道具界面
        ChallengeSuccessReward,//成功广告
        RewardSuccessReward,//奖励广告
        force_reward,//放弃奖励广告
    }

 

    /// <summary>
    /// 奖励类型
    /// </summary>
    public enum ERewardType
    {
        Money = 0,
        Gem = 1,
        AddBottle = 2,
        Clear = 3,
        Undo = 4,
        Prompt = 5,
        Guid = 6,
        LuckyWalletMoney = 7,
    }
    //广告类型
    public enum EAdtype
    {
        Reward,//激励
        Interstitial,//插屏
        Banner,//横幅
    }
    /// <summary>
    /// 转盘奖励类型（参考表LuckySpin.xlsx）
    /// </summary>
    public enum ELuckySpinRewardType : int
    {
        Money = 0,
        Gem = 1,
        AddBottle = 2,
        Clear = 3,
        Undo = 4,
        Prompt = 5,
        Guid = 6,
        LuckyWalletMoney = 7,
    }

    /// <summary>
    /// 奖励类型特效用结构体
    /// </summary>
    public struct ERewardItemStruct
    {
        public ERewardType Type;
        public float Count;
    }
    #endregion
}
