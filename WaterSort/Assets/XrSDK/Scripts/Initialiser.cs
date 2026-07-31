#pragma warning disable 0649

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using XrCode;


namespace XrSDK
{
    [DefaultExecutionOrder(-999)]
    public class Initialiser : Singleton<Initialiser>,ILoad
    {
        private static ProjectInitSettings initSettings;

        public Initialiser()
        {
        }

        public static bool IsInititalized { get; private set; }

        public void Load()
        {
            //DontDestroyOnLoad(gameObject);

            if (!IsInititalized)
            {
                IsInititalized = true;

                initSettings = Resources.Load<ProjectInitSettings>("Settings/Project Init Settings");

                initSettings.Initialise(this);
            }
        }

        public static bool IsModuleInitialised(Type moduleType)
        {
            ProjectInitSettings projectInitSettings = initSettings;

            BaseModulePendant[] coreModules = null;
            BaseModulePendant[] initModules = null;

#if UNITY_EDITOR
            if (!IsInititalized)
            {
                projectInitSettings = RuntimeEditorUtils.GetAssetByName<ProjectInitSettings>();
            }
#endif

            if (projectInitSettings != null)
            {
                coreModules = projectInitSettings.CoreModules;
                initModules = projectInitSettings.Modules;
            }

            for (int i = 0; i < coreModules.Length; i++)
            {
                if (coreModules[i].GetType() == moduleType)
                {
                    return true;
                }
            }

            for (int i = 0; i < initModules.Length; i++)
            {
                if (initModules[i].GetType() == moduleType)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            IsInititalized = false;
        }
    }
}
