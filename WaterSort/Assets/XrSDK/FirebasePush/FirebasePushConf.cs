namespace XrSDK
{
    public class FirebasePushConf
    {
        public FirebasePushBackendMode backendMode = FirebasePushBackendMode.FirebaseCloud;
        public bool requestPermissionOnStart = false;
        public bool subscribeDefaultTopic = true;
        public string defaultTopic = "all_users";

        // FirebaseCloud
        public bool enableAnonymousAuth = true;
        public bool saveTokenToFirestore = true;
        public string usersCollection = "users";

        // LocalServer
        public string localTokenRegisterUrl = "";
        public bool reportTokenToLocalServer = true;
    }
}
