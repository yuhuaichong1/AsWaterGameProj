using System;
using System.Collections.Generic;

[Serializable]
public class LevelConfig
{
    public int level;
    public List<CupDataJson> cups;
}

[Serializable]
public class CupDataJson
{
    public float x;
    public float y;
    public List<int> colors;
    public int whNums;
    public int whMask;
    public int isVideo;
    public int isLock;
    public int lockColor;
    public int lockNums;
    public int isNull;
    public int isEmptyCup;
}