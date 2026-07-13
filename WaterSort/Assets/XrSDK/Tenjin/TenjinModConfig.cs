using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using XrSDK;

namespace XrSDK
{
    [RegisterModule("Tenjin Module")]
    public class TenjinModConfig : BaseModulePendant
    {
        public override string ModuleName => "Tenjin";

        public override void CreateModule()
        {
            TenjinConf data = new TenjinConf();


            TenjinFrame module = new TenjinFrame(data);
            module.Load();
        }
    }
}
