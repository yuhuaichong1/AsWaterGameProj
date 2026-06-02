#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using UnityEngine;

public class SAndroidToastHelper
{
    private AndroidJavaObject toastInstance; // Android工具类实例
    private AndroidToastItem defaultATI;

    public SAndroidToastHelper()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        toastInstance = new AndroidJavaObject("com.sandroidplugin.unitytoast.AndroidToast", currentActivity);

        if (defaultATI == null)
        {
            defaultATI = new AndroidToastItem()
            {
                tipFontSize = 24,
                tipFontColor = new Color32(255, 255, 255, 255),
                tipFontStype = FontStyle.Normal,

                bgWidth = 950,
                bgHeight = 550,
                bgColor = new Color32(0, 0, 0, 175),

                outlineWidth = 5,
                outlineColor = new Color32(255, 255, 255, 255)
            };
        }
    }

    public void PlayToast(string toastText, int showDuration = 3000, bool ifAdaptive = false, AndroidToastItem item = null)
    {
        AndroidToastItem tStyle = item == null ? defaultATI : item;

        toastInstance.Call("ShowToast",
            toastText, ifAdaptive, showDuration,
                tStyle.tipFontSize, Color32ToHex6(tStyle.tipFontColor), (int)(tStyle.tipFontStype),
                tStyle.bgWidth, tStyle.bgHeight, Color32ToHex6(tStyle.bgColor), (int)(tStyle.bgColor.a),
                tStyle.outlineWidth, Color32ToHex6(tStyle.outlineColor));
    }

    /// <summary>
    /// 将Color32转换为#XXXXXX的形式
    /// </summary>
    /// <param name="color32">颜色</param>
    /// <returns>#XXXXXX</returns>
    private string Color32ToHex6(Color32 color32)
    {
        return $"#{color32.r.ToString("X2")}{color32.g.ToString("X2")}{color32.b.ToString("X2")}";
    }
}
#endif
