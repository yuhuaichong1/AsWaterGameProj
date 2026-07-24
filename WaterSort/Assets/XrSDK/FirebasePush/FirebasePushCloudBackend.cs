#if FIREBASE_USE_CLOUD_BACKEND
using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using XrCode;

namespace XrSDK
{
    /// <summary>
    /// Firebase Auth + Firestore 上报。仅在定义了 FIREBASE_USE_CLOUD_BACKEND 且导入 Auth/Firestore 包时编译。
    /// </summary>
    internal static class FirebasePushCloudBackend
    {
        private const string PrefsUidKey = "firebase_uid";

        public static void EnsureUserThen(FirebasePushConf conf, Action<string> onUidReady)
        {
            if (!conf.enableAnonymousAuth)
            {
                onUidReady?.Invoke(null);
                return;
            }

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser != null)
            {
                string uid = auth.CurrentUser.UserId;
                SaveUid(uid);
                Debug.Log("[FirebasePush] Reuse anonymous uid: " + uid);
                onUidReady?.Invoke(uid);
                return;
            }

            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[FirebasePush] Anonymous sign-in failed: " + task.Exception);
                    onUidReady?.Invoke(null);
                    return;
                }

                string uid = auth.CurrentUser != null ? auth.CurrentUser.UserId : null;
                SaveUid(uid);
                Debug.Log("[FirebasePush] Anonymous sign-in ok, uid: " + uid);
                onUidReady?.Invoke(uid);
            });
        }

        public static void SaveToken(FirebasePushConf conf, string token, string firebaseUid)
        {
            if (!conf.saveTokenToFirestore)
                return;

            string docId = !string.IsNullOrEmpty(firebaseUid)
                ? firebaseUid
                : ("device_" + SystemInfo.deviceUniqueIdentifier);

            string gameUserId = null;
            try
            {
                gameUserId = FacadePlayer.GetPlayerID?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FirebasePush] GetPlayerID failed: " + e.Message);
            }

            var data = new Dictionary<string, object>
            {
                { "fcmToken", token },
                { "platform", FirebasePushFrame.GetPlatformNamePublic() },
                { "pushEnabled", true },
                { "appVersion", Application.version },
                { "updatedAt", FieldValue.ServerTimestamp }
            };

            if (!string.IsNullOrEmpty(gameUserId))
                data["gameUserId"] = gameUserId;

            if (!string.IsNullOrEmpty(firebaseUid))
                data["firebaseUid"] = firebaseUid;

            DocumentReference doc = FirebaseFirestore.DefaultInstance
                .Collection(conf.usersCollection)
                .Document(docId);

            doc.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError("[FirebasePush] Firestore write failed: " + task.Exception);
                else
                    Debug.Log("[FirebasePush] Token saved to Firestore users/" + docId);
            });
        }

        private static void SaveUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return;

            PlayerPrefs.SetString(PrefsUidKey, uid);
            PlayerPrefs.Save();
        }
    }
}
#endif
