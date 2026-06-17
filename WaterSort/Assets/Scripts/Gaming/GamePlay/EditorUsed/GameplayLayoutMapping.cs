using UnityEngine;

namespace AsGame.Core
{
    /// <summary>
    /// 关卡 JSON 坐标与运行时一致：直接作为 CupPart 内的 localPosition（1:1，无缩放）。
    /// 运行时 GamePlayModule 摆放公式为 瓶底 = CupPart中心 + (x, y)（中心由预制体决定）。
    /// 编辑器预览以「局内区域(绿框)」中心为原点，按同样的 1:1 偏移摆放，使预览与游戏一致。
    /// 绿框（GameplayScreenLayoutData 的边距）需对齐游戏 CupPart 区域，必要时在编辑器里微调上下边距。
    /// </summary>
    public static class GameplayLayoutMapping
    {
        public static Vector2 CupToPlayArea(Vector2 cupPosition, GameplayScreenLayoutData layout)
        {
            layout ??= GameplayScreenLayout.Default;
            var play = GameplayScreenLayout.GetPlayAreaRect(layout);
            // 1:1：cup 坐标即相对绿框中心的偏移，与运行时 CupPart 摆放完全一致。
            return play.center + cupPosition;
        }

        public static Vector2 PlayAreaToCup(Vector2 playPosition, GameplayScreenLayoutData layout)
        {
            layout ??= GameplayScreenLayout.Default;
            var play = GameplayScreenLayout.GetPlayAreaRect(layout);
            return playPosition - play.center;
        }
    }
}
