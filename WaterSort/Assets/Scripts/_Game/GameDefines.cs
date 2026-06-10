/* 
 * 游戏定义脚本
 * 游戏中常量、枚举等内容请在这里统一定义
 */

using System.Collections.Generic;
using UnityEngine;

public abstract class GameDefines
{
    #region 常量

    #region 打包相关
    public static string URL = "http://api.game1231.top:8080/xgame?appIndex=77";                            //后台链接网址
    public static bool ifIAA = true;                                                                        //是否为IAA模式
    public static bool ifDebug = true;                                                                      //是否是debug模式
    public static bool ifSkipAD = true;                                                                     //是否跳过广告
    #endregion

    #region 加载延后提示UI相关
    public static float loadwait_delay = 15;
    public static string loadwait_content = "Network issue detected\nPlease wait a moment";
    #endregion

    #region 游戏相关
    public static string NameString = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";               //随机名称数组

    public static float Elimination_Money = 0.2f;                                                           //单次消除金额
    public static float IAA_Elimination_Money = 1f;                                                         //单次消除金额（IAA）

    public static int miniLevel_Start = 9;                                                                  //迷你关开始关号
    public static int miniLevel_End = 17;                                                                   //迷你关结束关号

    public static float RefreshATime = 0.05f;                                                               //刷新功能特效间隔时间
    public static int RefreshLCount = 8;                                                                    //刷新功能特效重复次数

    public static Dictionary<int, string> dai_ziName = new Dictionary<int, string>()
    {
        {0, ""},
        {1, "cheng"},
        {2, "huang"},
        {3, "lv"},
        {4, "sheng_lan"},
        {5, "lan"},
        {6, "zi"},
        {7, "hui"},
        {8, "hong"},
    };                    //颜色id对应的袋子spine动画皮肤名称

    #endregion

    #region 特效相关

    public static float GRE_StayTime = 1f;                                                                  //获取奖励特效持续时间（同时也是之后的飞物体特效的延迟时间）
    public static int GRE_FlyMoneyCount = 8;                                                                //奖励特效执行飞行钱特效时飞钱的数量
    public static float LTE_MoveTime = 0.5f;                                                                //关卡目标特效移入时间
    public static float LTE_StayTime = 2f;                                                                  //关卡目标特效持续时间
    public static float LTE_GoAwayTime = 0.5f;                                                              //关卡目标特效移出时间
    public static float CE_MoveTime = 0.2f;                                                                 //祝贺特效移入时间
    public static float CE_StayTime = 1f;                                                                   //祝贺特效持续时间
    public static Vector2 CE_Content_attemptTimes = new Vector2(1, 3);                                      //祝贺特效内容随机尝试次数
    public static float FlyProp_MoveTime = 0.5f;                                                            //飞行道具持续时间
    public static int FlyMoney_FlyMoneyCount = 8;                                                           //单次消除后飞行钱特效的数量
    public static float FlyMoney_MoveTime = 0.5f;                                                           //飞行钱持续时间
    public static float FlyMoney_DelayTime = 0.5f;                                                          //飞行钱延迟播放时间
    public static float FlyMoney_IntervalTime = 0.1f;                                                       //飞行钱间隔时间
    public static float FlyMoney_RandomSpawnDist = 100f;                                                    //飞行钱随机生成位置的区间
    public static float FlyMoneyTip_DelayTime = 1f;                                                         //飞行钱提示延迟播放时间
    public static float FlyMoneyTip_MoveTime = 2f;                                                          //飞行钱提示持续时间
    public static float FlyMoneyTip_MoveDist = 60f;                                                         //飞行钱提示移动距离
    public static float DifficultyUp_StayTime = 1;                                                          //难度提升特效持续时间
    public static Vector2 TargetArriveTime = new Vector2(1, 5);                                             //关卡目标特效随机时间

    #endregion

    #region 兑现相关

    public static int LP_PackCount = 200;                                                                   //幸运玩家包数
    public static int LP_PlayerNo = 8;                                                                      //幸运玩家名次

    public static Vector2 Withdrawal_RQuota = new Vector2(1000, 2000);                                      //可兑现金额随机区间

    public static int[] WithdrawalLevels = new int[3] { 1, 2, 17 };                                         //可兑现关卡
    public static float MinWithdrawalAmount = 3000;                                                         //最低兑现金额
    public static int DiffVal = 300;                                                                        //补钱阈值

    public static int CheckInDay = 8;                                                                       //签到领奖天数
    public static int CheckInLevel = 5;                                                                     //签到所需关卡

    public static Vector2 HighValue = new Vector2(200, 1000);                                               //高价值提现区间

    #endregion

    #region UI打开、关闭动画时间

    public static float ShowAnimTime = 0.25f;                                                               //UI动画打开持续时间
    public static float HideAnimTime = 0.25f;                                                               //UI动画关闭持续时间

    #endregion

    #region 默认语言&支付方式及其相关

    public static string Default_Channels = "0";                                                            //默认支付方式
    public static string Default_Mark = "$";                                                                //默认货币符号
    public static int Default_Decimal = 2;                                                                  //默认小数点位
    public static int Default_ExchangeRate = 1;                                                             //默认汇率
    public static ELanguageType Default_Language = ELanguageType.English;                                   //默认语言
    public static int Default_NANP = 1;                                                                     //默认国际长途电话区号
    public static string Default_Language2 = "English";                                                     //默认语言（标题用）

    #endregion

    #region 下三道具功能默认数量

    public static int Default_Prop1_Count = 3;                                                              //道具1“A”默认数量
    public static int Default_Prop2_Count = 3;                                                              //道具2“B”默认数量
    public static int Default_Prop3_Count = 3;                                                              //道具3“C”默认数量

    #endregion

    #region 各种路径

    public static string LevelsDataPath = "Json/Levels/ConfTotal.json";                                     //关卡数据路径
    public static string BottlePath = "Prefabs/Game/Bottle.prefab";                                         //瓶子预制体路径
    public static string BottleShadowPath = "Prefabs/Game/BottleShadow.prefab";                             //瓶子阴影预制体路径
    public static string PocketPath = "Prefabs/Game/Pocket.prefab";                                         //饮料预制体路径
    public static string RefreshEffectPath = "Prefabs/Game/RefreshEffect.prefab";                           //刷新功能特效
    public static string ShuihuaEffectPath = "Prefabs/Game/ShuihuaEffect.prefab";                           //倒水时的水花特效

    #region 奖励特效小图标路径

    public static string ERMoneyIconPath = "Sprites/UI/Money/Middle/icon_qianbidui.png";                    //三叠钱
    public static string ERIAAMoneyIconPath = "Sprites/UI/Money/Middle/IAAStars.png";                       //一叠硬币
    public static string ERProp1IconPath = "UI/FuncIcon/SFuncIcon_Prop1.png";                               //刷新道具
    public static string ERProp2IconPath = "UI/FuncIcon/SFuncIcon_Prop2.png";                               //回退道具
    public static string ERProp3IconPath = "UI/FuncIcon/SFuncIcon_Prop3.png";                               //添加瓶子道具

    #endregion

    #endregion

    #region 玩家相关
    public static int Default_MaxEnergy = 5;                                                                //默认体力
    public static float Default_Energy_RecoverTime = 600;                                                   //默认体力恢复时间（秒）
    public static int Default_Energy_TimeAdd = 1;                                                           //计时结束后所能恢复的体力

    public static int EXP_Single = 2;                                                                       //单次消除获取的经验
    #endregion

    #region 新手引导相关

    public static int firstGuideId = 10001;                                                                 //第一次新手引导步骤                                             

    #endregion

    #region 关卡通过相关

    public static Vector2 LevelComplate_RandomRange = new Vector2(20f, 45f);                                //奖励区间

    #endregion

    #region 幸运奖励界面相关

    public static float ClockTime1 = 30;                                                                    //每经过X秒，在点击水瓶后弹一次弹窗
    public static float ClockTime2 = 60;                                                                    //每经过X秒，在点击水瓶后弹一次弹窗
    public static Vector2 LuckyReward_RandomRange = new Vector2(20f, 45f);                                  //奖励区间
    public static int ClockLv = 17;                                                                         //当<=X关时，用ClockTime1，>X关后，用ClockTime2，

    #endregion

    #region 幸运转盘相关

    public static int SpinCount = 30;                                                                       //每完成X次线轴，弹一次弹窗
    public static List<float> LS_Angles = new List<float>() { 33, 95, 153, 210, 266, 327 };                 //转盘对应角度
    public static int LS_rotateCount = 6;                                                                   //旋转圈数
    public static float LS_rotateTime = 2f;                                                                 //转完所需时间
    public static float RewardCoe = 3;                                                                      //默认情况下的转盘金钱奖励系数

    #endregion

    #region 广告相关

    public static Vector2 WeightAdRange = new Vector2(1, 101);                                              //权重广告随机区间
    public static int AdWeight = 50;                                                                        //权重广告随机分界（小于等于为激励，大于为插屏）
    public static int AdRefuseCount = 3;                                                                    //广告拒绝次数

    #endregion

    #endregion
}

#region 枚举

/// <summary>
/// 游戏状态
/// </summary>
public enum EGameState
{
    Load,             //加载游戏
    Start,            //开始游戏
    Run,              //运行游戏
    Pause,            //暂停游戏
    Exit,             //退出游戏
}

/// <summary>
/// UI类型枚举  和ui表配置一一对应
/// </summary>
public enum EUIType
{
    ENone = 0,
    EUIEffect = 1,
    EUIGamePlay = 2,
    EUIGuide = 3,
    EUILevelCompleted = 4,
    EUILevelFailure = 5,
    EUILoading = 6,
    EUILuckyReward = 7,
    EUILuckySpin = 8,
    EUINotice = 9,
    EUIProp = 10,
    EUIReStart = 11,
    EUISetting = 12,
    EUITask = 13,
    EUIUserLevel = 14,
    EUIWithdrawConfirm = 15,
    EUIWithdrawEnterInfo = 16,
    EUIWithdrawFeedback = 17,
    EUIWithdrawGoal = 18,
    EUIWithdrawLuckyPlayer = 19,
    EUIWithdrawProgress = 20,
    EUIWithdrawRecords = 21,
    EUIWithdrawContinue = 22,
    EUIWithdrawKeepEarn = 23,
    EUIWithdrawTarget = 24,
}

/// <summary>
/// 场景类型枚举
/// </summary>
public enum ESceneType : byte
{
    MainScene = 1,          //主城场景
    Meun = 2,               //主菜单      
}

/// <summary>
/// UI状态
/// </summary>
public enum EUIState
{
    EUIHiding,
    EUIOpened,
    EUIClosed,
}

/// <summary>
/// 监听接口
/// </summary>
public enum EMsgCode : int
{
    EC2S_Login = 1001, //登录
    ES2C_Login = 1002,
}

/// <summary>
/// 多语言
/// </summary>
public enum ELanguageType : int
{
    None = 0,
    Chinese_s = 1,
    Chinese_t = 2,
    English = 3,
    German = 4,
    Japanese = 5,
    Brazilian_Portuguese = 6,
    French = 7,
    Spanish = 8,
    Korean = 9,
    Indonesian = 10,
    Russian = 11,
    Hindi = 12,
    Thai = 13,
    Turkish = 14,
    Arabic = 15,

    LengthTest = 99,
}

/// <summary>
/// 下三功能
/// </summary>
public enum EFuncType : int
{
    Prop1 = 0,
    Prop2 = 1,
    Prop3 = 2,
}

/// <summary>
/// 广告来源
/// </summary>
public enum EAdSource
{
    Prop,//道具界面
    LevelCompleted,//关卡结算
    LevelFailureTryAgain,//关卡失败重来
    LuckyReward,//幸运一刻
    UnlockPocket,//解锁饮料位
    UnlockBottle,//解锁瓶子位
    Refuse_LuckyReward,//拒绝观看广告——幸运奖励
    Refuse_LevelComplate,//拒绝观看广告——关卡完成
}

/// <summary>
/// 广告类型
/// </summary>
public enum EAdType
{
    Reward,//激励
    Interstitial,//插屏
    Banner,//横幅
    AppOpen,//开屏
}

/// <summary>
/// 支付类型（与表格PayChannel的sn一一对应）
/// </summary>
public enum EPayType : int
{
    None = -1,
    PayPal = 0,
    Venmo = 1,
    Zelle = 2,
    E_Transfer = 3,
    Mercado = 4,
    BBVA = 5,
    Pix = 6,
    PicPay = 7,
    Skrill = 8,
    Revolut = 9,
    Sofort = 10,
    Paylib = 11,
    PayPay = 12,
    CashApp = 13,
    Other = 14,
}

/// <summary>
/// 支付所填写的信息的类型（与表格PayChannel的infoType一一对应）
/// </summary>
public enum EPOEType : int
{  
    Email = 1,//邮箱
    Phone = 2,//电话
    POE = 3,//电话or邮箱
}

/// <summary>
/// 兑现订单状态
/// </summary>
public enum EWOrderState
{
    Processing,//进行中
    Finish,//已完成
    Error,//错误
}

/// <summary>
/// 奖励类型
/// </summary>
public enum ERewardType
{
    Money = 0,
    Prop1 = 1,
    Prop2 = 2,
    Prop3 = 3,
}

/// <summary>
/// 提现订单类型
/// </summary>
public enum EWithRecordState : int
{
    GoWithdrawal = 0,//去提款
    UnderReview = 1,//审核中
}

/// <summary>
/// 需要刷新的体力的部分
/// </summary>
public enum EShowEnergyType
{
    Energy,
    Time,
    All,
}

/// <summary>
/// 兑现目标类型
/// </summary>
public enum WithdrawTarget : int
{
    PassLevel,//通过第X关
    AmountOfMoney,//目标金额
    CheckIn,//签到
}

/// <summary>
/// 游戏内状态
/// </summary>
public enum GameStatus
{
    None = 0,
    Ready = 1,
    Gaming = 2,
    Moving = 3,
    UsingProp = 4,
    Lose = 5,
    Win = 6,
    Pause = 7,
    Over = 8
}

/// <summary>
/// 转盘奖励类型（参考表LuckySpin.xlsx）
/// </summary>
public enum ELuckySpinRewardType : int
{
    Money = 0,
    Refresh = 1,
    Undo = 2,
    AddBottle = 3,
}

/// <summary>
/// 目标界面
/// </summary>
public enum UIWTOpenType : int
{
    FirstTarget = 0,
    AVPTarget,
    VPTarget,
    SVPTarget,
    SVPMoneyTarget,
    FinishTarget1,
    FinishTarget2,
}

#endregion

#region 结构体

/// <summary>
/// 奖励类型特效用结构体
/// </summary>
public struct ERewardItemStruct
{
    public ERewardType Type;
    public float Count;
}

#endregion
