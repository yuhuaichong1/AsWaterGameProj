using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XrSDK
{
    [RegisterModule("YRT")]
    public class YRTPendant : BaseModulePendant
    {
        public override string ModuleName => "YRT_SDK";

        public override void CreateModule()
        {
            YRTData data = new YRTData();

            YRTModule module = new YRTModule(data);
            module.Load();
        }
    }
}
