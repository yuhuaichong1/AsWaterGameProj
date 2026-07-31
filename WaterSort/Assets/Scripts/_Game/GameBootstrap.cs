using System;

namespace XrCode
{
    /// <summary>
    /// 游戏启动钩子。可选模块（如 XrSDK）在 RuntimeInitializeOnLoadMethod 中自行订阅，
    /// Game 只负责触发，不依赖具体类型。
    /// </summary>
    public static class GameBootstrap
    {
        public static Action OnPreLoad;
    }
}
