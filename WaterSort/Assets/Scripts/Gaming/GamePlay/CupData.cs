using System;
using System.Collections.Generic;
using UnityEngine;

namespace XrCode
{
    [Serializable]
    public class CupData
    {
        public int id;
        public Vector2 position;
        public List<int> colors = new List<int>();
        public int whNums;
        /// <summary>按位标记 L0..L3 是否为问号层；0 时回退使用 whNums 连续隐藏。</summary>
        public int whMask;
        public int isVideo;
        public int isLock;
        public int lockColor;
        public int lockNums;
        public int isNull;
        /// <summary>1=空玻璃瓶（局内有瓶，默认无水；编辑器刷新水层不参与）。</summary>
        public int isEmptyCup;

        public CupData Clone()
        {
            return new CupData
            {
                id = id,
                position = position,
                colors = new List<int>(colors),
                whNums = whNums,
                whMask = whMask,
                isVideo = isVideo,
                isLock = isLock,
                lockColor = lockColor,
                lockNums = lockNums,
                isNull = isNull,
                isEmptyCup = isEmptyCup
            };
        }
    }

    [Serializable]
    public class PourActionRecord
    {
        public int fromId;
        public int toId;
        public int colorId;
        public int num;
    }

    [Serializable]
    public class LevelConfigRoot
    {
        // JsonUtility 不支持 Dictionary，使用 LevelConfigLoader 解析
    }
}
