using UnityEngine;

//UIOCType

/// <summary>
/// UI开启方式类型
/// </summary>
public enum UIOpenType
{
    None,//无（不参与）
    Cover,//覆盖
    Replace,//替换
    Back,//递归返回
}

/// <summary>
/// UI关闭方式类型
/// </summary>
public enum UICloseType
{
    None,//无（不参与）
    Pop,//消去当前二级UI
    Clear,//清除所有二级UI

}