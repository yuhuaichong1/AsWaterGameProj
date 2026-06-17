using UnityEngine;

namespace XrSDK
{
    //模块挂件基类
    public abstract class BaseModConfig : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        public string moduleName;

        public BaseModConfig()
        {
            moduleName = " Default Module";
        }
        //运行时创建模块
        public abstract void CreateModule();
    }
}