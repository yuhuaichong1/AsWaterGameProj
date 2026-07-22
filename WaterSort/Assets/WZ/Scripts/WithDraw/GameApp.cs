using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WZSDK
{
    //统一定义游戏中的管理器，在此类进行初始化
    public class GameApp : SingleTon<GameApp>
    {
        public static ViewManager viewManager;//视图管理器
        public static BasePanel BasePanel;//视图注册

        public override void Init()
        {
            viewManager = new ViewManager();
            BasePanel = new BasePanel();
        }
    }
}