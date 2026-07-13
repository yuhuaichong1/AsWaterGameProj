using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using XrCode;

namespace XrSDK
{
    public class TenjinFrame : BaseModule
    {
        public TenjinFrame(TenjinConf data)
        {
            if (data != null)
            {
                
            }
        }

        protected override void OnLoad()
        {
            Init();
        }

        private void Init()
        {
            AddEvent();
        }

        /// <summary>
        /// 添加接口
        /// </summary>
        private void AddEvent()
        {
            //FacadeTenjinExtend. += ;
        }

        /// <summary>
        /// 移除接口
        /// </summary>
        private void RemoveEvent()
        {
            //FacadeTenjinExtend. -= ;
        }

        protected override void OnDispose()
        {
            RemoveEvent();
        }
    }
}

