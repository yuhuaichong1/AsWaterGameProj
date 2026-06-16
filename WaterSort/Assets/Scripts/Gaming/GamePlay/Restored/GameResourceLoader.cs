using UnityEngine;

namespace AsGame.Core
{
    /// <summary>
    /// 加载 Resources 下的 Sprite。贴图未设为 Sprite 类型时，从 Texture2D 动态创建。
    /// </summary>
    public static class GameResourceLoader
    {
        public static Sprite LoadSprite(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)) return null;

            var sprite = Resources.Load<Sprite>(resourcesPath);
            if (sprite != null) return sprite;

            var tex = Resources.Load<Texture2D>(resourcesPath);
            if (tex == null) return null;

            return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
