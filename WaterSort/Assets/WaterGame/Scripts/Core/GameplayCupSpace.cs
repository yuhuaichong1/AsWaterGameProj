using UnityEngine;

namespace AsGame.Core
{
    /// <summary>
    /// 运行时瓶子坐标系：与 GameplayCanvas 中 CupMgr（750×1334，中心锚点）一致。
    /// 关卡 JSON 的 x/y 即此空间下的本地坐标。关卡编辑器预览会映射到 1200×2132 的「局内边距」绿框。
    /// </summary>
    public static class GameplayCupSpace
    {
        public const float CupAreaWidth = 750f;
        public const float CupAreaHeight = 1334f;
        public const float HalfCupAreaWidth = CupAreaWidth * 0.5f;
        public const float HalfCupAreaHeight = CupAreaHeight * 0.5f;

        /// <summary>瓶子可摆放区域（中心原点，与 cupRoot localPosition 一致）。</summary>
        public static Rect CupAreaRect =>
            Rect.MinMaxRect(-HalfCupAreaWidth, -HalfCupAreaHeight, HalfCupAreaWidth, HalfCupAreaHeight);

        /// <summary>将瓶子底边锚点限制在 CupMgr 区域内。</summary>
        public static Vector2 ClampCupPosition(Vector2 position) =>
            ClampCupPosition(position, GameConstants.BottleWidth, GameConstants.BottleHeight);

        public static Vector2 ClampCupPosition(Vector2 position, float bottleWidth, float bottleHeight)
        {
            var area = CupAreaRect;
            var halfW = bottleWidth * 0.5f;
            var minX = area.xMin + halfW;
            var maxX = area.xMax - halfW;
            var minY = area.yMin;
            var maxY = area.yMax - bottleHeight;
            return new Vector2(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY));
        }
    }
}
