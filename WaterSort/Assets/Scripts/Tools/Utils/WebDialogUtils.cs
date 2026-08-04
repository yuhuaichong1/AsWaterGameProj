using UnityEngine;

namespace XrCode
{
    public static class WebDialogUtils
    {
        public static void Open(string url)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ShowAndroidDialog(url);
#else
            Application.OpenURL(url);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ShowAndroidDialog(string url)
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject dialog = new AndroidJavaObject("android.app.Dialog", activity);
                    dialog.Call("setTitle", "Privacy Policy");

                    AndroidJavaObject layout = new AndroidJavaObject("android.widget.LinearLayout", activity);
                    layout.Call("setOrientation", 1);

                    AndroidJavaObject webView = new AndroidJavaObject("android.webkit.WebView", activity);
                    AndroidJavaObject settings = webView.Call<AndroidJavaObject>("getSettings");
                    settings.Call("setJavaScriptEnabled", true);
                    settings.Call("setDomStorageEnabled", true);

                    AndroidJavaObject webLayoutParams = new AndroidJavaObject(
                        "android.widget.LinearLayout$LayoutParams",
                        -1,
                        0,
                        1.0f);
                    layout.Call("addView", webView, webLayoutParams);

                    AndroidJavaObject closeButton = new AndroidJavaObject("android.widget.Button", activity);
                    closeButton.Call("setText", "Close");
                    closeButton.Call("setOnClickListener", new KxAndClickLsn(() =>
                    {
                        dialog.Call("dismiss");
                    }));

                    AndroidJavaObject buttonLayoutParams = new AndroidJavaObject(
                        "android.widget.LinearLayout$LayoutParams",
                        -1,
                        DpToPx(activity, 48));
                    layout.Call("addView", closeButton, buttonLayoutParams);

                    dialog.Call("setContentView", layout);
                    dialog.Call("show");

                    AndroidJavaObject metrics = activity.Call<AndroidJavaObject>("getResources")
                        .Call<AndroidJavaObject>("getDisplayMetrics");
                    int width = metrics.Get<int>("widthPixels");
                    int height = metrics.Get<int>("heightPixels");
                    AndroidJavaObject window = dialog.Call<AndroidJavaObject>("getWindow");
                    window?.Call("setLayout", Mathf.RoundToInt(width * 0.9f), Mathf.RoundToInt(height * 0.85f));

                    webView.Call("loadUrl", url);
                }));
            }
        }

        private static int DpToPx(AndroidJavaObject activity, int dp)
        {
            AndroidJavaObject metrics = activity.Call<AndroidJavaObject>("getResources")
                .Call<AndroidJavaObject>("getDisplayMetrics");
            float density = metrics.Get<float>("density");
            return Mathf.RoundToInt(dp * density);
        }

        private class KxAndClickLsn : AndroidJavaProxy
        {
            private readonly System.Action clickAction;

            public KxAndClickLsn(System.Action onClick) : base("android.view.View$OnClickListener")
            {
                clickAction = onClick;
            }

            public void onClick(AndroidJavaObject view)
            {
                clickAction?.Invoke();
            }
        }
#endif
    }
}
