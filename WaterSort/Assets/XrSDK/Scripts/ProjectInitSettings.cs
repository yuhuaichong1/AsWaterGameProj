#pragma warning disable 0649

using UnityEngine;

namespace XrSDK
{
    [SetupTab("Init Settings", priority = 1, texture = "icon_puzzle")]
    [CreateAssetMenu(fileName = "Project Init Settings", menuName = "Settings/Project Init Settings")]
    public class ProjectInitSettings : ScriptableObject
    {
        [SerializeField] BaseModulePendant[] coreModules;
        public BaseModulePendant[] CoreModules => coreModules;

        [SerializeField] BaseModulePendant[] modules;
        public BaseModulePendant[] Modules => modules;

        public void Initialise(Initialiser initialiser)
        {
            for (int i = 0; i < coreModules.Length; i++)
            {
                if(coreModules[i] != null)
                {
                    coreModules[i].CreateModule();
                }
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if(modules[i] != null)
                {
                    modules[i].CreateModule();
                }
            }
        }
    }
}