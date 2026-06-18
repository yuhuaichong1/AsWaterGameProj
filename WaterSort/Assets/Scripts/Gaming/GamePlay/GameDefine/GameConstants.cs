using System.Collections.Generic;
using UnityEngine;

namespace XrCode
{
    /// <summary>从 Cocos Constant.js 移植的常量。</summary>
    public static class GameConstants
    {
        public const float BottleWidth = 114f;
        public const float HalfBottleWidth = BottleWidth * 0.5f;
        public const float BottleHeight = 303f;
        public const float HalfBottleHeight = BottleHeight * 0.5f;
        public const float BottleShadowDiffX = -12f;
        public const float BottleShadowDiffY = 47f;
        public const float GridHeight = 47f;
        // 底层无向下溢出，需比 GridHeight 多 UpperOverflow(24) 才能与上层视觉等高。
        public const float GridOneHeight = 71f;
        public static readonly float[] BottleAngles = { 95f, 78f, 66f, 54f, 42f };
        public static readonly float[] WaterMaxY = { 0f, 71f, 118f, 165f, 212f, 230f };
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

        /// <summary>装袋 Spine 相对口袋节点的偏移（对齐 Cocos GamePocketSpinePos）。</summary>
        public static readonly Dictionary<int, Vector2> GamePocketSpinePos = new Dictionary<int, Vector2>
        {
            { 0, Vector2.zero },
            { 1, new Vector2(10f, 114f) },
            { 2, new Vector2(20f, 118f) },
            { 3, new Vector2(18f, 118f) },
            { 4, new Vector2(18f, 118f) },
            { 5, new Vector2(11f, 115f) },
            { 6, new Vector2(17f, 117f) },
            { 7, new Vector2(26f, 117f) },
            { 8, new Vector2(5f, 114f) },
        };

        /// <summary>装袋 dai_zi Spine 皮肤名（对齐 Cocos GamePocketSpineSkin）。</summary>
        public static readonly Dictionary<int, string> GamePocketSpineSkin = new Dictionary<int, string>
        {
            { 0, "" },
            { 1, "cheng" },
            { 2, "huang" },
            { 3, "lv" },
            { 4, "sheng_lan" },
            { 5, "lan" },
            { 6, "zi" },
            { 7, "hui" },
            { 8, "hong" },
        };

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
