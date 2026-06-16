using UnityEngine;
using XrCode;

namespace AsGame.Core
{
    /// <summary>运行时瓶子坐标系：与 CupMgr 本地坐标一致。</summary>
    public static class GameplayCupSpace
    {
        public const float CupAreaWidth = 750f;
        public const float CupAreaHeight = 1334f;
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
            return new Vector2(
                Mathf.Clamp(position.x, area.xMin + halfW, area.xMax - halfW),
                Mathf.Clamp(position.y, area.yMin, area.yMax - bottleHeight));
        }
    }
}
