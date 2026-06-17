using UnityEngine;

namespace XrSDK
{
    //模块挂件基类
    public abstract class BaseModulePendant : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        protected string moduleName;
        //模块挂件名称  --  用于显示
        public abstract string ModuleName { get; }
        //运行时创建模块
        public abstract void CreateModule();
    }
}