using System;
using UnityEngine;
using XrCode;

namespace AsGame.Core
{
    /// <summary>
    /// 局内瓶子摆放区域（设计分辨率下的边距 → 中心原点 UI 坐标矩形）。
    /// 默认设计分辨率 1200×2132：上距顶 800、下距底 240、左右 0。
    /// </summary>
    [Serializable]
    public class GameplayScreenLayoutData
    {
        public float designWidth = 1200f;
        public float designHeight = 2132f;
        public float insetTop = 800f;
        public float insetBottom = 240f;
        public float insetLeft = 0f;
        public float insetRight = 0f;

        public GameplayScreenLayoutData Clone() => new GameplayScreenLayoutData
        {
            designWidth = designWidth,
            designHeight = designHeight,
            insetTop = insetTop,
            insetBottom = insetBottom,
            insetLeft = insetLeft,
            insetRight = insetRight
        };
    }

    public static class GameplayScreenLayout
    {
        public static readonly GameplayScreenLayoutData Default = new();

        /// <summary>设计分辨率下、以屏幕中心为原点的局内区域（xMin,yMin 为左下角）。</summary>
        public static Rect GetPlayAreaRect(GameplayScreenLayoutData layout, float? width = null, float? height = null)
        {
            layout ??= Default;
            var w = width ?? layout.designWidth;
            var h = height ?? layout.designHeight;
            var scaleX = w / layout.designWidth;
            var scaleY = h / layout.designHeight;

            var halfW = w * 0.5f;
            var halfH = h * 0.5f;
            var minX = -halfW + layout.insetLeft * scaleX;
            var maxX = halfW - layout.insetRight * scaleX;
            var maxY = halfH - layout.insetTop * scaleY;
            var minY = -halfH + layout.insetBottom * scaleY;
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        public static Rect GetFullScreenRect(float width, float height) =>
            Rect.MinMaxRect(-width * 0.5f, -height * 0.5f, width * 0.5f, height * 0.5f);

        /// <summary>将瓶子底边锚点限制在局内区域内（与 GameController 坐标一致）。</summary>
        public static Vector2 ClampCupPosition(Vector2 position, GameplayScreenLayoutData layout,
            float screenWidth, float screenHeight)
        {
            var area = GetPlayAreaRect(layout, screenWidth, screenHeight);
            var halfW = GameConstants.BottleWidth * 0.5f;
            var minX = area.xMin + halfW;
            var maxX = area.xMax - halfW;
            var minY = area.yMin;
            var maxY = area.yMax - GameConstants.BottleHeight;
            return new Vector2(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY));
        }

        public static Vector2 ClampCupPosition(Vector2 position, GameplayScreenLayoutData layout) =>
            ClampCupPosition(position, layout, layout.designWidth, layout.designHeight);
    }
}
