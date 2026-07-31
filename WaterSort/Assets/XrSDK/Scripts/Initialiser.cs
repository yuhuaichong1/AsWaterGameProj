#pragma warning disable 0649

using System;
using UnityEngine;
using XrCode;


namespace XrSDK
{
    public class Initialiser : Singleton<Initialiser>, ILoad
    {
        private static ProjectInitSettings initSettings;

        public Initialiser()
        {
        }

        public static bool IsInititalized { get; private set; }

        public void Load()
        {
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

            if (coreModules != null)
            {
                for (int i = 0; i < coreModules.Length; i++)
                {
                    if (coreModules[i] != null && coreModules[i].GetType() == moduleType)
                    {
                        return true;
                    }
                }
            }

            if (initModules != null)
            {
                for (int i = 0; i < initModules.Length; i++)
                {
                    if (initModules[i] != null && initModules[i].GetType() == moduleType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
