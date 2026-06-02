using UnityEngine;

namespace AsGame.UI.PrefabGen
{
    /// <summary>
    /// UI 脚本实现此接口后，编辑器工具会在 Content 节点下自动搭建层级并保存为 Prefab。
    /// 仅用于 Editor 生成；运行时仍可使用 Prefab 或代码动态 UI。
    /// </summary>
    public interface IUIPrefabBlueprint
    {
        /// <summary>在 content 根节点下创建子节点、绑定 SerializeField 等。</summary>
        void BuildPrefabUI(UIPrefabBuildContext context);
    }
}
