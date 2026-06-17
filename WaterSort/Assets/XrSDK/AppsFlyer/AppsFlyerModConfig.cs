using AppsFlyerSDK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XrSDK
{
    [RegisterModule("AppsFlyer Module")]
    public class AppsFlyerModConfig : BaseModulePendant
    {

        public string MonoObjPath = "";
        public string DevKey = "";
        public string AppID = "";
        public string UWPAppID = "";
        public string MacOSAppID = "";
        public bool IsDebug = false;
        public bool GetConversionData = false;

        public override string ModuleName => "AppsFlyer";

        public override void CreateModule()
        {
            AppsFlyerConf data = new AppsFlyerConf();
            data.monoObjPath = MonoObjPath;
            data.devKey = DevKey;
            data.appID = AppID;
            data.UWPAppID = UWPAppID;
            data.macOSAppID = MacOSAppID;
            data.isDebug = IsDebug;
            data.getConversionData = GetConversionData;     
#if UNITYEDITOR
            return;
#endif
            AppsFlyerFrame module = new AppsFlyerFrame(data);
            module.Load();
        }
    }

}