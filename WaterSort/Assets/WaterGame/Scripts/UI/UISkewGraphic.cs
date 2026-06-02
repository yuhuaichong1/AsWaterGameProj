using UnityEngine;
using UnityEngine.UI;

namespace AsGame.UI
{
    /// <summary>模拟 Cocos cc.Node.skewY，用于倒水时液面倾斜。</summary>
    [RequireComponent(typeof(Graphic))]
    public class UISkewGraphic : BaseMeshEffect
    {
        [Range(-89f, 0f)] public float skewY;

        public void SetSkewY(float value)
        {
            skewY = value;
            if (TryGetComponent(out Graphic g))
                g.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Mathf.Approximately(skewY, 0f)) return;

            // Cocos 的 skewY：按 X 坐标错切 Y（让水平的水面随瓶倾斜），
            // 与父级 content 的反向旋转叠加后，水面在世界空间保持水平。
            var tan = Mathf.Tan(skewY * Mathf.Deg2Rad);
            var vert = new UIVertex();
            for (var i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);
                vert.position.y += tan * vert.position.x;
                vh.SetUIVertex(vert, i);
            }
        }
    }
}
