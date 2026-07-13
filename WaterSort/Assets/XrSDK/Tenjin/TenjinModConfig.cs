using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using XrSDK;

/*
 * ！！！！！！！
 * implementation 'com.tenjin:android-sdk:1.16.7'要用这个版本，否则就升到Unity6000打包（导入的SDK（对应的是github中的1.15.14）也要对应版本）
 * 需要在“AndroidManifest.xml”中添加以下权限
 * <uses-permission android:name="android.permission.INTERNET" />
 * <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
 * 如果目标API级别为33 (Android 13) 或更高，需要此权限来获取广告ID(额，可能不用？)
 * <uses-permission android:name="com.google.android.gms.permission.AD_ID" />
 * 可能需要添加的内容：
 * 	<uses-permission android:name="android.permission.ACCESS_WIFI_STATE"/>
	<uses-permission android:name="android.permission.WAKE_LOCK" />
 */

namespace XrSDK
{
    [RegisterModule("Tenjin Module")]
    public class TenjinModConfig : BaseModulePendant
    {
        public int AppSubversioin;
        public string SDK;
        public AppStoreType appStoreType;
        public bool ifDebug;
        public bool ifOptIn = true;
        public bool ifUseCMP;

        public override string ModuleName => "Tenjin";

        public override void CreateModule()
        {
            TenjinConf data = new TenjinConf();
            data.AppSubversioin = AppSubversioin;
            data.SDK = SDK;
            data.appStoreType = appStoreType;
            data.ifDebug = ifDebug;
            data.ifOptIn = ifOptIn;
            data.ifUseCMP = ifUseCMP;

            TenjinFrame module = new TenjinFrame(data);
            module.Load();
        }
    }
}
