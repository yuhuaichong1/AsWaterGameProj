namespace WZSDK
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    
    //单例
    public class SingleTon<T>
    {
       private static readonly T instance = Activator.CreateInstance<T>();
    
       public static T Instance
       {
          get
          {
             return instance;
          }
       }
    
       public virtual void Init()
       {
          
       }
    
       //每帧执行
       public virtual void Update(float dt)
       {
          
       }
    
       //销毁
       public virtual void OnDestroy()
       {
          
       }
    }
}
