using UnityEngine;

namespace AsGame.Core
{
    /// <summary>
    /// 关卡 JSON 使用 CupMgr(750×1334) 坐标；局内预览按「屏幕与局内区域」边距矩形显示。
    /// 二者通过线性映射对齐，使编辑器绿框与 Game 中瓶子相对 HUD/道具区的位置一致。
    /// </summary>
    public static class GameplayLayoutMapping
    {
        public static Vector2 CupToPlayArea(Vector2 cupPosition, GameplayScreenLayoutData layout)
        {
            layout ??= GameplayScreenLayout.Default;
            var play = GameplayScreenLayout.GetPlayAreaRect(layout);
            var cup = GameplayCupSpace.CupAreaRect;
            var tx = Mathf.InverseLerp(cup.xMin, cup.xMax, cupPosition.x);
            var ty = Mathf.InverseLerp(cup.yMin, cup.yMax, cupPosition.y);
            return new Vector2(
                Mathf.Lerp(play.xMin, play.xMax, tx),
                Mathf.Lerp(play.yMin, play.yMax, ty));
        }

        public static Vector2 PlayAreaToCup(Vector2 playPosition, GameplayScreenLayoutData layout)
        {
            layout ??= GameplayScreenLayout.Default;
            var play = GameplayScreenLayout.GetPlayAreaRect(layout);
            var cup = GameplayCupSpace.CupAreaRect;
            var tx = Mathf.InverseLerp(play.xMin, play.xMax, playPosition.x);
            var ty = Mathf.InverseLerp(play.yMin, play.yMax, playPosition.y);
            return new Vector2(
                Mathf.Lerp(cup.xMin, cup.xMax, tx),
                Mathf.Lerp(cup.yMin, cup.yMax, ty));
        }
    }
}
