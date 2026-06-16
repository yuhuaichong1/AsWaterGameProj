using System.Collections.Generic;
using AsGame.Core;
using UnityEngine;
using XrCode;

namespace AsGame.Editor.LevelEditor
{
    /// <summary>
    /// 局内预览绿框网格摆放（与 LevelEditorWindow 虚线网格一致）。
    /// 用户行号：1=最上排，5=最下排；水平方向隔一格摆一瓶（间距为一瓶宽）。
    /// </summary>
    public static class LevelGridPlacement
    {
        public const int UserRowCount = 5;
        public const float DefaultCellWidth = GameConstants.BottleWidth;
        public const float DefaultCellHeight = 245f;

        /// <summary>生成 N 个瓶子的局内区域底边锚点（设计 UI 坐标），再映射为 CupMgr 坐标。</summary>
        public static List<Vector2> BuildCupPositions(
            int bottleCount,
            GameplayScreenLayoutData layout = null,
            float cellWidth = DefaultCellWidth)
        {
            layout ??= GameplayScreenLayout.Default;
            var play = GameplayScreenLayout.GetPlayAreaRect(layout);
            var cellH = play.height / UserRowCount;
            var playPoints = BuildPlayAreaPoints(bottleCount, play, cellWidth, cellH);

            var result = new List<Vector2>(playPoints.Count);
            foreach (var p in playPoints)
            {
                var cup = GameplayLayoutMapping.PlayAreaToCup(p, layout);
                cup = GameplayCupSpace.ClampCupPosition(cup, cellWidth, cellH);
                result.Add(cup);
            }

            return result;
        }

        /// <summary>局内区域坐标系下的摆放点（y 为瓶底）。</summary>
        public static List<Vector2> BuildPlayAreaPoints(
            int bottleCount, Rect play, float cellWidth, float cellHeight)
        {
            var points = new List<Vector2>(bottleCount);
            if (bottleCount <= 0) return points;

            var totalCols = Mathf.Max(1, Mathf.FloorToInt(play.width / cellWidth));
            foreach (var (userRow, count) in PlanRows(bottleCount))
            {
                var cols = CenteredColumns(count, totalCols);
                var y = RowBottomY(userRow, play, cellHeight);
                foreach (var col in cols)
                {
                    var x = play.xMin + (col + 0.5f) * cellWidth;
                    points.Add(new Vector2(x, y));
                }
            }

            return points;
        }

        /// <summary>N&lt;=6 全第3排；6&lt;N&lt;=12 第2/4排；12&lt;N&lt;=18 第1/3/5排。</summary>
        public static List<(int userRow, int count)> PlanRows(int bottleCount)
        {
            var plan = new List<(int, int)>();
            if (bottleCount <= 0) return plan;

            if (bottleCount <= 6)
            {
                plan.Add((3, bottleCount));
                return plan;
            }

            if (bottleCount <= 12)
            {
                var row2 = bottleCount / 2;
                plan.Add((2, row2));
                plan.Add((4, bottleCount - row2));
                return plan;
            }

            var row1 = bottleCount / 3;
            var row3 = bottleCount / 3;
            plan.Add((1, row1));
            plan.Add((3, row3));
            plan.Add((5, bottleCount - row1 - row3));
            return plan;
        }

        /// <summary>userRow 1=顶排，5=底排。</summary>
        static float RowBottomY(int userRow, Rect play, float cellHeight)
        {
            userRow = Mathf.Clamp(userRow, 1, UserRowCount);
            var indexFromBottom = UserRowCount - userRow;
            return play.yMin + indexFromBottom * cellHeight;
        }

        /// <summary>水平居中；相邻瓶列索引差 2（中间空一格）。</summary>
        static List<int> CenteredColumns(int bottleCount, int totalCols)
        {
            var span = bottleCount > 0 ? 2 * bottleCount - 1 : 0;
            var start = Mathf.Max(0, (totalCols - span) / 2);
            var cols = new List<int>(bottleCount);
            for (var i = 0; i < bottleCount; i++)
                cols.Add(start + i * 2);
            return cols;
        }
    }
}
