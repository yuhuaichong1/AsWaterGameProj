using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WZSDK
{
    //“Ù∆µ∂®“Â¿‡
    public static class FacadeAudioExtend
    {
        public static Action PlayBgmHandle;
        public static Action StopBgmHandle;
        public static Action<AudioStCategory> PlayEffectHandle;
        public static Action PlayVibrateHandle;
        public static Action<float> SetMusicVolumeHandle;
        public static Action<float> SetEffectsVolumeHandle;
        public static Action<bool> SetVibrateHandle;
        public static Func<float> GetMusicVolumeHandle;
        public static Func<float> GetEffectsVolumeHandle;
        public static Func<bool> GetVibrateHandle;
        public static Func<string, AudioStCategory> GetEATypeByStrHandle;
    }

}