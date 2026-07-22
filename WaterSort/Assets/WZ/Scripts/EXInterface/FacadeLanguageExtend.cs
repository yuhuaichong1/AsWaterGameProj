using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WZSDK
{
    public static class FacadeLanguageExtend
    {
        public static Action<int> OnLanguageChangeHandle;                 //多语言类型变更
        public static Func<string, string> GetTextHandle;                 //获取对应多语言
    }
}