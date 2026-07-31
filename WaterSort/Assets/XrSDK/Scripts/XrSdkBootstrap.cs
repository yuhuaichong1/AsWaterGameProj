using UnityEngine;
using XrCode;

namespace XrSDK
{
    /// <summary>
    /// 有 XrSDK 的分支会编译此文件，并在首场景 Awake 前挂上 PreLoad 钩子。
    /// </summary>
    public static class XrSdkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            GameBootstrap.OnPreLoad += OnGamePreLoad;
        }

        private static void OnGamePreLoad()
        {
            // Singleton.Instance 首次访问时会调用 ILoad.Load()
            _ = Initialiser.Instance;
        }
    }
}
