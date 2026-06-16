using UnityEngine;
using AsGame.Core;
using AsGame.UI;

namespace XrCode
{
    /// <summary>从 Cocos CupComp.rotateTo / calculateTriangleSides 移植。</summary>
    public static class BottlePourMath
    {
        public struct TriangleSides
        {
            public float Opposite;
        }

        public static TriangleSides CalculateTriangleSides(float angleDeg)
        {
            if (angleDeg < 0.5f)
                return new TriangleSides { Opposite = 0f };
            var adjacent = CalculateTriangleAdjacent(angleDeg, 20f);
            var rad = angleDeg * Mathf.Deg2Rad;
            return new TriangleSides { Opposite = adjacent * Mathf.Sin(rad) };
        }

        static float CalculateTriangleAdjacent(float angleDeg, float side)
        {
            if (angleDeg < 0.5f) return 0f;
            var rad = angleDeg * Mathf.Deg2Rad;
            var tan = Mathf.Tan(rad);
            if (Mathf.Abs(tan) < 1e-4f) return 0f;
            return side / tan;
        }

        public static Vector3 PourPosition(Vector3 anchor, float angleDeg, int dir)
        {
            var tri = CalculateTriangleSides(angleDeg);
            return anchor + new Vector3(tri.Opposite * dir, 150f, 0f);
        }

        public static void ApplyRotateTo(
            float angleDeg,
            RectTransform content,
            RectTransform[] waterContainers,
            UISkewGraphic[] waterSkews,
            int visibleWaterCount)
        {
            if (content != null)
                content.localRotation = angleDeg < 0.5f
                    ? Quaternion.identity
                    : Quaternion.Euler(0, 0, -angleDeg);

            if (angleDeg < 0.5f)
            {
                if (waterSkews != null)
                {
                    foreach (var skew in waterSkews)
                        skew?.SetSkewY(0f);
                }
                return;
            }

            var totalH = 4f * GameConstants.GridHeight + 20f;
            var h3 = totalH / 4f - 3f;
            var h2 = totalH / 3f - 6f;
            var h1 = totalH / 2f - 9f;
            var h0 = totalH / 2f + 60f;

            var hide3 = angleDeg >= GameConstants.BottleAngles[3];
            var hide2 = angleDeg >= GameConstants.BottleAngles[2];
            var hide1 = angleDeg >= GameConstants.BottleAngles[1];

            var lowHeight = 0f;
            if (angleDeg < 30f)
                lowHeight = GameConstants.GridHeight + 4f * Mathf.Clamp01(angleDeg / 15f);

            var stackY = 0f;
            for (var i = 0; i <= 3; i++)
            {
                if (waterContainers == null || i >= waterContainers.Length || waterContainers[i] == null)
                    continue;

                var container = waterContainers[i];
                var active = i < visibleWaterCount;
                if (hide3 && i == 3) active = false;
                if (hide2 && i == 2) active = false;
                if (hide1 && i == 1) active = false;
                container.gameObject.SetActive(active);
                if (!active) continue;

                var skew = waterSkews != null && i < waterSkews.Length ? waterSkews[i] : null;
                if (skew != null)
                    skew.SetSkewY(Mathf.Clamp(-angleDeg, -86f, 0f));

                float height;
                float offsetX = 0f;

                if (angleDeg <= 30f)
                {
                    height = lowHeight;
                }
                else if (angleDeg <= GameConstants.BottleAngles[3])
                {
                    var from = GameConstants.BottleAngles[3];
                    var to = GameConstants.BottleAngles[4];
                    var span = from - to;
                    height = i == 3
                        ? h3 * Mathf.Clamp01((from - angleDeg) / span)
                        : Mathf.Lerp(h3, h2, Mathf.Clamp01((angleDeg - to) / span));
                }
                else if (angleDeg <= GameConstants.BottleAngles[2])
                {
                    var from = GameConstants.BottleAngles[2];
                    var to = GameConstants.BottleAngles[3];
                    var span = from - to;
                    height = i == 2
                        ? h2 * Mathf.Clamp01((from - angleDeg) / span)
                        : Mathf.Lerp(h2, h1, Mathf.Clamp01((angleDeg - to) / span));
                }
                else if (angleDeg <= GameConstants.BottleAngles[1])
                {
                    var from = GameConstants.BottleAngles[1];
                    var to = GameConstants.BottleAngles[2];
                    var span = from - to;
                    height = i == 1
                        ? h1 * Mathf.Clamp01((from - angleDeg) / span)
                        : Mathf.Lerp(h1, h0, Mathf.Clamp01((angleDeg - to) / span));
                }
                else
                {
                    var from = GameConstants.BottleAngles[0];
                    var to = GameConstants.BottleAngles[1];
                    var span = from - to;
                    height = h0 * Mathf.Clamp01((from - angleDeg) / span);
                    if (angleDeg > 84f)
                        offsetX = (angleDeg - 84f) / 5.5f * -40f;
                }

                height = Mathf.Max(height, 0.01f);
                container.sizeDelta = new Vector2(container.sizeDelta.x, height);
                container.anchoredPosition = new Vector2(offsetX, stackY);
                stackY += height;
            }
        }
    }
}
