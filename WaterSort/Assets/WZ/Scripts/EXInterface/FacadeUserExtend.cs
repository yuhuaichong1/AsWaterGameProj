using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WZSDK
{
    // 登录模块外部接口定义
    public static class FacadeUserExtend
    {
        public static Func<float> GetUserCoinHandle;                  
        public static Action<float> SetUserCoinHandle;                
        public static Action<float> AddUserCoinHandle;                
        public static Func<int> GetLevelHandle;                   
        public static Action<int> SetLevelHandle;                 
        public static Action<int> AddLevelHandle;                 
        public static Func<int> GetTempLevelHandle;               
        public static Action<int> SetTempLevelHandle;             
        public static Func<int> GetSelectedPaintingHandle;        
        public static Action<int> SetSelectedPaintingHandle;      
        public static Func<int> GetPropCountHandle;                 
        public static Action<int> SetPropCountHandle;               
        public static Func<int> GetSkinIDHandle;                  
        public static Action<int> SetSkinIDHandle;                
        public static Func<int> GetAddSpacePropNumHandle;         
        public static Action<int> SetAddSpacePropCountHandle;       
        public static Action<int> AddAddSpacePropNumHandle;       
        public static Func<int> GetClearPropNumHandle;            
        public static Action<int> SetClearPropNumHandle;          
        public static Action<int> AddClearPropNumHandle;          
        public static Func<int> GetHammerPropNumHandle;           
        public static Action<int> SetHammerPropNumHandle;         
        public static Action<int> AddHammerPropNumHandle;         
        public static Action<int> AddExpHandle;                   
        public static Func<int> GetExpHandle;                     
        public static Func<int> GetUserLevelHandle;               
        public static Func<string> GetUserIDHandle;               
        public static Func<string> GetUserNameHandle;             
        public static Func<float> GetWithdrawalCoinHandle;        
        public static Action<float> AddWithdrawalCoinHandle;      
    }

}