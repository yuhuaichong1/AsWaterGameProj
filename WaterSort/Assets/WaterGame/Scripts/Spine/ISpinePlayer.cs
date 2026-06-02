using System;
using UnityEngine;

namespace AsGame.Spine
{
    /// <summary>
    /// Spine 播放抽象。安装 spine-unity 后实现 <see cref="SpineUnityPlayer"/> 替换 <see cref="SpinePlayerStub"/>。
    /// </summary>
    public interface ISpinePlayer
    {
        void Play(Transform host, string skeletonResourcePath, string animationName, bool loop, Action onComplete = null,
            Color? tint = null, float? duration = null);
        void SetSkin(Transform host, string skinName);
        void Clear(Transform host);
    }
}
