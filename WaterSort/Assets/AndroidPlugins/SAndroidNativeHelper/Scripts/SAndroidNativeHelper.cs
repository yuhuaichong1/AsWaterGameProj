using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
/* 通知功能“Notification”
 * 使用前需要通过“RequestNotificationPermission”方法询问用户开放通知权限
 *
 * 小图片存放路径为“Assets/Plugins/Android/res/drawable/”
 * 小图片仅支持“透明背景 + 白色图样”
 *
 * 其他前置需求：
 * AndroidManifest.xml中开放权限：
 *     <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
 * 	   <uses-permission android:name="android.permission.USE_FULL_SCREEN_INTENT" android:maxSdkVersion="32" />
 * mainTemplate.gradle中引入“androidx”相关内容：
 *     implementation 'androidx.core:core:1.7.0'
 *     implementation 'androidx.appcompat:appcompat:1.4.0'
*/

/*震动功能“PlayVibration”
 *
 * 其他前置需求：
 * AndroidManifest.xml中开放权限：
 *     <uses-permission android:name="android.permission.VIBRATE" />
*/

public class SAndroidNativeHelper
{
    private AndroidJavaObject notificationJavaObj;
    private AndroidJavaObject vibrationJavaObj;

    public void CreatedInstance()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        notificationJavaObj = new AndroidJavaObject("com.sandroidplugin.unitynotification.AndroidNotification", currentActivity);
        vibrationJavaObj = new AndroidJavaObject("com.sandroidplugin.unityvibration.AndroidVibration", currentActivity);
    }

    public void RequestNotificationPermission()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            int sdkInt = buildVersion.GetStatic<int>("SDK_INT");

            if (sdkInt >= 33)
            {
                using (AndroidJavaClass permissionChecker = new AndroidJavaClass("androidx.core.app.ActivityCompat"))
                using (AndroidJavaClass permission = new AndroidJavaClass("android.Manifest$permission"))
                {
                    string perm = permission.GetStatic<string>("POST_NOTIFICATIONS");
                    int result = permissionChecker.CallStatic<int>("checkSelfPermission",
                        currentActivity, perm);

                    if (result != 0)
                    {
                        permissionChecker.CallStatic("requestPermissions",
                            currentActivity,
                            new string[] { perm },
                            1001);
                    }
                }
            }
        }
    }

    public void Notification(int noticeId, string title, string content, string iconName = "")
    {
        notificationJavaObj.Call("ShowNotification", noticeId, title, content, iconName);
    }

    public void PlayVibration(long milliseconds = 10, int amplitude = -1)
    {
        if (amplitude > 255)
        {
            Debug.LogWarning("The maximum vibration intensity is 255");
            amplitude = 255;
        }

        vibrationJavaObj.Call("PlayVibration", milliseconds, amplitude);
    }

    public void CancelVibration()
    {
        vibrationJavaObj.Call("CancelVibration");
    }
}
#else
/// <summary>非 Android 平台占位，避免编辑器编译失败。</summary>
public class SAndroidNativeHelper
{
    public void CreatedInstance() { }

    public void RequestNotificationPermission() { }

    public void Notification(int noticeId, string title, string content, string iconName = "") { }

    public void PlayVibration(long milliseconds = 10, int amplitude = -1) { }

    public void CancelVibration() { }
}
#endif
