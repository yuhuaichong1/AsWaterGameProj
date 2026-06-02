using UnityEngine;
using AsGame.Water;
using AsGame.UI;

namespace AsGame.Scenes
{
    public static class GameSceneBuilder
    {
        public static void Build()
        {
            if (Object.FindObjectOfType<GameController>() != null) return;

            var go = ResourcePrefabLoader.Instantiate(PrefabPaths.GameplayCanvas);
            if (go == null) return;
            go.name = "GameplayCanvas";
        }
    }
}
