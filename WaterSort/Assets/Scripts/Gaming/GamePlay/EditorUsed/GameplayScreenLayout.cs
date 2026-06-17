using System;
using UnityEngine;
using XrCode;

namespace AsGame.Core
{
    [Serializable]
    public class GameplayScreenLayoutData
    {
        public float designWidth = 1200f;
        public float designHeight = 2132f;
        // 绿框=游戏 CupPart 区域：由预制体 UIGamePlay 的 CupPart 链路推算（约 x[-600,600]、y[-744,213]）。
        // 若与实机仍有少量竖直偏差，可在编辑器面板微调上下边距。
        public float insetTop = 853f;
        public float insetBottom = 322f;
        public float insetLeft = 0f;
        public float insetRight = 0f;

        public GameplayScreenLayoutData Clone() => new()
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

        public static Rect GetPlayAreaRect(GameplayScreenLayoutData layout, float? width = null, float? height = null)
        {
            layout ??= Default;
            var w = width ?? layout.designWidth;
            var h = height ?? layout.designHeight;
            var scaleX = w / layout.designWidth;
            var scaleY = h / layout.designHeight;
            return Rect.MinMaxRect(
                -w * 0.5f + layout.insetLeft * scaleX,
                -h * 0.5f + layout.insetBottom * scaleY,
                w * 0.5f - layout.insetRight * scaleX,
                h * 0.5f - layout.insetTop * scaleY);
        }

        public static Rect GetFullScreenRect(float width, float height) =>
            Rect.MinMaxRect(-width * 0.5f, -height * 0.5f, width * 0.5f, height * 0.5f);

        public static Vector2 ClampCupPosition(
            Vector2 position, GameplayScreenLayoutData layout, float screenWidth, float screenHeight)
        {
            var area = GetPlayAreaRect(layout, screenWidth, screenHeight);
            var halfW = GameConstants.BottleWidth * 0.5f;
            return new Vector2(
                Mathf.Clamp(position.x, area.xMin + halfW, area.xMax - halfW),
                Mathf.Clamp(position.y, area.yMin, area.yMax - GameConstants.BottleHeight));
        }

        public static Vector2 ClampCupPosition(Vector2 position, GameplayScreenLayoutData layout) =>
            ClampCupPosition(position, layout, layout.designWidth, layout.designHeight);
    }
}
