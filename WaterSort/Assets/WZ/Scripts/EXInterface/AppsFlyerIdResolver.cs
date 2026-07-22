using System;
using AppsFlyerSDK;
using UnityEngine;

namespace WZSDK
{
    public static class AppsFlyerIdResolver
    {
        private static string editorFallbackAfDeviceId;

        public static string ResolveCurrentId(bool allowEditorFallback = true)
        {
            if (!string.IsNullOrEmpty(FacadeAppsFlyerExtend.CurrentAppsFlyerId))
            {
                return FacadeAppsFlyerExtend.CurrentAppsFlyerId;
            }

            try
            {
                string appsFlyerId = AppsFlyer.getAppsFlyerId();
                if (!string.IsNullOrEmpty(appsFlyerId))
                {
                    FacadeAppsFlyerExtend.NotifyAppsFlyerIdReady(appsFlyerId);
                    return appsFlyerId;
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"AppsFlyer ID direct lookup failed in editor: {e.Message}");
#endif
            }

#if UNITY_EDITOR
            if (allowEditorFallback)
            {
                if (string.IsNullOrEmpty(editorFallbackAfDeviceId))
                {
                    editorFallbackAfDeviceId = $"editor-af-{Guid.NewGuid():N}";
                    Debug.LogWarning($"AppsFlyer ID unavailable in editor, using fallback: {editorFallbackAfDeviceId}");
                }

                FacadeAppsFlyerExtend.NotifyAppsFlyerIdReady(editorFallbackAfDeviceId);
                return editorFallbackAfDeviceId;
            }
#endif

            return string.Empty;
        }
    }
}
