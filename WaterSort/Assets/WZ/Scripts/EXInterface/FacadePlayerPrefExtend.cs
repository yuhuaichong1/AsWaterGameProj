using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WZSDK
{
    public abstract class FacadePlayerPrefExtend
    {
        private static string GetKey(string baseKey)
        {
            if (GameDefines.ifIAA)
            {
                return $"IAA_{baseKey}";
            }
            return baseKey;
        }
        public static string MusicKey = GetKey("Music");
        public static string SoundKey = GetKey("Sound");
        public static string HapticKey = GetKey("Haptic");
        public static string LegacyMusicKey =  GetKey("State" + MusicKey);
        public static string LegacySoundKey =  GetKey("State" + SoundKey);
        public static string LegacyHapticKey =  GetKey("State" + HapticKey);

        // 系统相关
        public static string ifIAA = GetKey("WaterPouringPuzzle_ifIAA");                                        // 是否为IAA模式
        public static string start = GetKey("Start");                                                           // 是否首次启动

        // 玩家信息相关
        public static string userLevel => GetKey("WaterPouringPuzzle_userLevel");                               // UserModule_玩家等级
        public static string userExp => GetKey("WaterPouringPuzzle_userExp");                                   // UserModule_玩家当前经验
        public static string userName => GetKey("WaterPouringPuzzle_userName");                                 // UserModule_玩家姓名
        public static string userID => GetKey("WaterPouringPuzzle_userID");                                     // UserModule_玩家ID
        public static string SuserID => GetKey("SuserID");                                     // UserModule_玩家ID
        public static string withdrawalCoin => GetKey("WaterPouringPuzzle_withdrawalCoin");                     // UserModule_玩家累计兑现金额

        // 游戏进度相关
        public static string currentLevel => GetKey("CurrentLevel");                                            // 当前关卡
        public static string restartNumber => GetKey("RestartNumber");                                          // 重玩次数
        public static string useBottles => GetKey("UseBottles");                                               // 使用的瓶子数量

        //消除次数相关
        public static string currentEliminationCount => GetKey("CurrentEliminationCount");                      // 当前轮次消除次数（达到40重置）
        public static string totalEliminationCount => GetKey("TotalEliminationCount");                          // 总消除次数（永久累积）

        // 货币相关
        public static string coin => GetKey("Coin");                                                            // 金币数量
        public static string gems => GetKey("Gems");                                                            // 钻石数量

        // 道具相关
        public static string undo => GetKey("Undo");                                                            // 撤销次数
        public static string AddBottleCount => GetKey("ADDBottleCount");                                        // add瓶子次数
        public static string AddName => GetKey("ADDName");                                                      // 名字
        public static string AddDay => GetKey("AddDay");                                                        // 天数
        public static string StageAdWatchCount => GetKey("StageAdWatchCount");                                  // 阶段广告观看次数
        public static string StageAdWatchOwnerStage => GetKey("StageAdWatchOwnerStage");                        // 阶段广告观看次数所属阶段
        public static string AddPhone => GetKey("ADDPhone");                                                    // 电话
        public static string AddEmail => GetKey("ADDEmail");                                                    // 邮箱
        public static string AddIndex => GetKey("ADDIndex");                                                    // 邮箱
        public static string History => GetKey("History");                                                      // 历史记录
        public static string WalletBalanceCache => GetKey("WalletBalanceCache");                                // 钱包余额缓存
        public static string WalletWithdrawOrdersCache => GetKey("WalletWithdrawOrdersCache");                  // 提现订单缓存
        public static string Stage => GetKey("Stage");                                                          // 当前阶段
        public static string ClearColorCount => GetKey("ClearColorCount");                                      // 清除次数

        // 阶段5登录追踪相关
        public static string Stage5_StartDay => GetKey("Stage5_StartDay");                                      // 进入阶段5时的起始登录天数
        public static string Stage5_CurrentCycle => GetKey("Stage5_CurrentCycle");                              // 阶段5当前周期进度

        public static string currentPalette => GetKey("CurrentPalette");                                        // 当前使用的调色板
                                                                                                                // 设置相关
        public static string music => GetKey("Music");                                                          // 音乐设置
        public static string haptic => GetKey("Haptic");                                                        // 震动设置

        // 教程相关
        public static string LevelTutorialShown => GetKey("LevelTutorialShown");                                // 关卡教程是否已显示（格式：LevelTutorialShown_关卡索引）

        public static string MaxUnlockedStage => GetKey("MaxUnlockedStage");                                    // 已经解锁的最大阶段
        public static string StageSystemVersion => GetKey("StageSystemVersion");                                // 阶段系统版本

        public static string LastLoginDateKey => GetKey("LastLoginDate");
        public static string LastLoginTimestampKey => GetKey("LastLoginTimestamp");
        public static string LoginDayCountKey => GetKey("LoginDayCount");
        public static string LoginDayHistoryKey => GetKey("LoginDayHistory");


        public static string HasShownRewardAtLevel3 => GetKey("HasShownRewardAtLevel3");
        public static string HasShownRewardAtLevel9 => GetKey("HasShownRewardAtLevel19");
        public static string HasShownLuckyWalletAtLevel => GetKey("HasShownLuckyWalletAtLevel7");


        public static string HasShownStage4 => GetKey("HasShownStage4");                                        // 是否已显示阶段4的提示
        public static string Stage5LuckySpinCompleted => GetKey("Stage5LuckySpinCompleted");                    // 阶段5补偿转盘是否已完成

        public static string LuckySpinWalletRewardClaimed => GetKey("LuckySpinWalletRewardClaimed");            // LuckySpin 小钱包首转奖励是否已领取

        public static string WithDrawExecuted3 => GetKey("WithDrawExecuted3");                                        // 第三次提现
    }
}
