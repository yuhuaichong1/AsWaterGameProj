using System.Collections.Generic;

namespace WZSDK
{
    public class YarnCoveMgr : Singleton<YarnCoveMgr>, ILoad, IDispose
    {
        public List<BaseModFrame> updateModList;

        private bool isLoaded = false;
        public void Load()
        {
       
            updateModList = new List<BaseModFrame>();
         
        }
        public void Dispose()
        {
        
            isLoaded = false;
          
        }

        public void Start()
        {
           
        }

        public void Update()
        {
            if (isLoaded)
            {
              
            }
        }

        public void FixedUpdate()
        {
            if (isLoaded)
            {
                //AssetBundleFrame.Instance.Update();
                for (int i = 0; i < updateModList.Count; i++)
                {
                    updateModList[i].FixedUpdate();
                }
            }
        }

        //×¢²á¸üÐÂÄ£¿é
        public void RegistUpdateObj(BaseModFrame module)
        {
            updateModList.Add(module);
        }
    }
}