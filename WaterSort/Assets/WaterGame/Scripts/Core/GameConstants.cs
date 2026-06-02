using System.Collections.Generic;
using UnityEngine;

namespace AsGame.Core
{
    /// <summary>从 Cocos Constant.js 移植的常量。</summary>
    public static class GameConstants
    {
        public const float BottleWidth = 91f;
        public const float HalfBottleWidth = BottleWidth * 0.5f;
        public const float BottleHeight = 242f;
        public const float HalfBottleHeight = BottleHeight * 0.5f;
        public const float BottleShadowDiffX = -12f;
        public const float BottleShadowDiffY = 47f;
        public const float GridHeight = 40f;
        public const float GridOneHeight = 60f;
        public static readonly float[] BottleAngles = { 95f, 78f, 66f, 54f, 42f };
        public static readonly float[] WaterMaxY = { 0f, 60f, 100f, 140f, 180f, 220f };
        public const int WaterMaxCount = 4;
        public const float MinAnimDuration = 0.1f;
        public const float MaxAnimDuration = 5f;

        public const int MaxHeart = 5;
        public const int HeartIntervalTime = 600;
        public static readonly int[] HeartInfiniteTime = { 30, 60 };

        public static readonly Dictionary<int, string> NewPlayUnlockHints = new Dictionary<int, string>
        {
            { 1, "打包任意奶茶可解锁白色标签瓶子" },
            { 2, "打包对应颜色奶茶才能解锁标签瓶子" },
            { 3, "倒出上方的水就能显现未知颜色" },
        };

        public static readonly int[] NewPlayUnlockLevels = { 3, 6, 7 };

        public static readonly Dictionary<int, ColorPair> GameColorData = new Dictionary<int, ColorPair>
        {
            { 0, new ColorPair("#4d4d4d", "#8b8b8b", "灰") },
            { 1, new ColorPair("#f6700f", "#ff9a28", "橙") },
            { 2, new ColorPair("#dac10d", "#fffa33", "黄") },
            { 3, new ColorPair("#01b700", "#33e833", "绿") },
            { 4, new ColorPair("#003cd2", "#4073f1", "蓝") },
            { 5, new ColorPair("#42b8f5", "#91ecff", "浅蓝") },
            { 6, new ColorPair("#3e006b", "#7b31b0", "紫") },
            { 7, new ColorPair("#744311", "#e2903c", "棕") },
            { 8, new ColorPair("#ff0507", "#ff6e6e", "红") },
        };

        public static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c))
                return c;
            return Color.white;
        }
    }

    public readonly struct ColorPair
    {
        public readonly string ColorHex;
        public readonly string ColorTopHex;
        public readonly string Name;

        public ColorPair(string color, string colorTop, string name)
        {
            ColorHex = color;
            ColorTopHex = colorTop;
            Name = name;
        }

        public Color Base => GameConstants.HexToColor(ColorHex);
        public Color Top => GameConstants.HexToColor(ColorTopHex);
    }
}
