using UnityEngine;
using XrCode;

namespace AsGame.Core
{
    /// <summary>
    /// 瓶子可摆放区（cup 坐标=相对局内区域中心的 1:1 偏移）。
    /// 尺寸对齐游戏 CupPart 区域（约 1200×957），用于拖拽/网格/批量生成的边界限制。
    /// </summary>
    public static class GameplayCupSpace
    {
        public const float CupAreaWidth = 1200f;
        public const float CupAreaHeight = 957f;
        public const float HalfCupAreaWidth = CupAreaWidth * 0.5f;
        public const float HalfCupAreaHeight = CupAreaHeight * 0.5f;

        public static Rect CupAreaRect =>
            Rect.MinMaxRect(-HalfCupAreaWidth, -HalfCupAreaHeight, HalfCupAreaWidth, HalfCupAreaHeight);

        public static Vector2 ClampCupPosition(Vector2 position) =>
            ClampCupPosition(position, GameConstants.BottleWidth, GameConstants.BottleHeight);

        public static Vector2 ClampCupPosition(Vector2 position, float bottleWidth, float bottleHeight)
        {
            var area = CupAreaRect;
            var halfW = bottleWidth * 0.5f;
            var halfH = bottleHeight * 0.5f;
            return new Vector2(
                Mathf.Clamp(position.x, area.xMin + halfW, area.xMax - halfW),
                Mathf.Clamp(position.y, area.yMin + halfH, area.yMax - halfH));
        }
    }
}
