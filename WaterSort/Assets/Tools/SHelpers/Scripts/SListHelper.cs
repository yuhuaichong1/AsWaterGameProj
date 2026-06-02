using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public static class SListHelper
{
    /// <summary>
    /// 洗牌算法
    /// </summary>
    /// <typeparam name="T">List的类型</typeparam>
    /// <param name="list">混淆完毕的List</param>
    public static void Shuffle<T>(this List<T> list)
    {
        int n = list.Count;
        // 在方法内部创建 Random 实例
        System.Random rng = new System.Random();
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    /// <summary>
    /// 在一定范围内取值（默认a<=x<b）,未比较的最小/大值将并入临近区间
    /// </summary>
    /// <typeparam name="T">可比较的类型</typeparam>
    /// <param name="list">范围间隔列表</param>
    /// <param name="value">目标值</param>
    /// <param name="type">比较方式</param>
    /// <returns>区间Id</returns>
    public static int GetRangeIndex<T>(this List<T> list, T value, RangeIndexType type = RangeIndexType.Before) where T : struct, IComparable<T>
    {
        if (list.Count <= 1)
        {
            Debug.LogWarning("list requires 2 or more values");
            if (list.Count == 1)
                return 0;
            else
                return -1;
        }

        List<T> resultList = list.Distinct().OrderBy(x => x).ToList();

        T minValue = resultList[0];
        T maxValue = resultList[resultList.Count - 1];

        if (value.CompareTo(minValue) < 0 || value.CompareTo(maxValue) > 0)
        {
            Debug.LogWarning("value is out of range");
            return -1;
        }
        if (value.CompareTo(minValue) == 0)
            return 0;
        if (value.CompareTo(maxValue) == 0)
            return resultList.Count - 2;

        int middleId = resultList.Count / 2;
        int index = FindRangeIndex(middleId, resultList, value, type);

        return index;
    }
    /// <summary>
    /// 二分法查找区间
    /// </summary>
    /// <typeparam name="T">可比较的类型</typeparam>
    /// <param name="middleId">中间Id</param>
    /// <param name="list">范围间隔列表</param>
    /// <param name="value">目标值</param>
    /// <param name="type">比较方式</param>
    /// <returns>区间Id</returns>
    private static int FindRangeIndex<T>(int middleId, List<T> list, T value, RangeIndexType type) where T : struct, IComparable<T>
    {
        int left = 0;
        int right = list.Count - 1;
        int targetIndex = -1;

        while (left <= right)
        {
            int mid = (left + right) / 2;
            int compareResult = value.CompareTo(list[mid]);

            switch (type)
            {
                case RangeIndexType.Before:
                    if (compareResult < 0)
                    {
                        right = mid - 1;
                        targetIndex = mid - 1;
                    }
                    else if (compareResult > 0)
                    {
                        left = mid + 1;
                    }
                    else
                    {
                        targetIndex = mid;
                        break;
                    }
                    break;

                case RangeIndexType.After:
                    if (compareResult <= 0)
                    {
                        right = mid - 1;
                        targetIndex = mid - 1;
                    }
                    else
                    {
                        left = mid + 1;
                    }
                    break;
            }
        }

        targetIndex = Mathf.Clamp(targetIndex, 0, list.Count - 2);

        return targetIndex;
    }
}


/// <summary>
/// 区间比较方式
/// </summary>
public enum RangeIndexType
{
    Before,//a<=x<b
    After,//a<x<=b
}
