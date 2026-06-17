using System.Collections;
using System.Collections.Generic;
using ThinkingData.Analytics;
using UnityEngine;

namespace XrSDK
{
    [RegisterModule("Thinking Data Module")]
    public class ThinkingDataModulePendant : BaseModulePendant
    {
        public bool EnableLog = false;
        public TDNetworkType NetworkType = TDNetworkType.All;
        public string APPID = "";
        public string SERVERURL = "";
        public TDMode MODE = TDMode.Normal;
        public TDTimeZone TimeZone = TDTimeZone.UTC;
        public TDAutoTrackEventType AutoTrackType = TDAutoTrackEventType.All;
        public bool AutoRL = true;

        public override string ModuleName => "ThinkingDataSDK";

        public override void CreateModule()
        {
            ThinkingDataData data = new ThinkingDataData();
            data.EnableLog = EnableLog;
            data.NetworkType = NetworkType;
            data.APPID = APPID;
            data.SERVERURL = SERVERURL;
            data.MODE = MODE;
            data.TimeZone = TimeZone;
            data.AutoTrackType = AutoTrackType;
            data.AutoRL = AutoRL;

            ThinkingDataModule module = new ThinkingDataModule(data);
            module.Load();
        }
    }
}