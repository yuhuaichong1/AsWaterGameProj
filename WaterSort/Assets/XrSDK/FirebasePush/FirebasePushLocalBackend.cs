using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using XrCode;

namespace XrSDK
{
    /// <summary>
    /// 自有服务器 Token 上报。不依赖 Firebase Auth / Firestore。
    /// </summary>
    internal static class FirebasePushLocalBackend
    {
        public static void SaveToken(FirebasePushConf conf, string token)
        {
            if (!conf.reportTokenToLocalServer)
                return;

            if (FacadeFirebasePush.UploadTokenToLocalServer != null)
            {
                try
                {
                    FacadeFirebasePush.UploadTokenToLocalServer.Invoke(token);
                    Debug.Log("[FirebasePush] Token handed to FacadeFirebasePush.UploadTokenToLocalServer");
                }
                catch (Exception e)
                {
                    Debug.LogError("[FirebasePush] UploadTokenToLocalServer failed: " + e);
                }
                return;
            }

            if (string.IsNullOrEmpty(conf.localTokenRegisterUrl))
            {
                Debug.LogWarning("[FirebasePush] LocalServer mode: LocalTokenRegisterUrl empty, skip auto upload. " +
                                 "Set URL in Project Init Settings or assign FacadeFirebasePush.UploadTokenToLocalServer.");
                return;
            }

            if (Game.Instance == null)
            {
                Debug.LogWarning("[FirebasePush] Game.Instance is null, cannot StartCoroutine for token upload.");
                return;
            }

            Game.Instance.StartCoroutine(PostToken(conf.localTokenRegisterUrl, token));
        }

        private static IEnumerator PostToken(string url, string token)
        {
            string gameUserId = null;
            try
            {
                gameUserId = FacadePlayer.GetPlayerID?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FirebasePush] GetPlayerID failed: " + e.Message);
            }

            string json =
                "{" +
                "\"userId\":\"" + Escape(gameUserId) + "\"," +
                "\"fcmToken\":\"" + Escape(token) + "\"," +
                "\"platform\":\"" + Escape(FirebasePushFrame.GetPlatformNamePublic()) + "\"," +
                "\"appVersion\":\"" + Escape(Application.version) + "\"," +
                "\"pushEnabled\":true" +
                "}";

            using (UnityWebRequest webReq = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                webReq.uploadHandler = new UploadHandlerRaw(body);
                webReq.downloadHandler = new DownloadHandlerBuffer();
                webReq.SetRequestHeader("Content-Type", "application/json");
                webReq.timeout = 30;

                Debug.Log("[FirebasePush] POST token to local server: " + url);
                yield return webReq.SendWebRequest();

                if (webReq.result == UnityWebRequest.Result.Success)
                    Debug.Log("[FirebasePush] Local server token register ok: " + webReq.downloadHandler.text);
                else
                    Debug.LogError("[FirebasePush] Local server token register failed: " + webReq.responseCode + " / " + webReq.error);
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
