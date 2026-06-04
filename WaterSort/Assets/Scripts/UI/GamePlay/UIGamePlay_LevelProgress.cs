using Spine.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGamePlay_LevelProgress : MonoBehaviour
{
    public List<UIGP_LP_Item> Items;
    public GameObject miniLevels;
    public Text miniLevelText;

    public void OnSetShow(int level)
    {

    }

    public void SetShowLevel2(int level)
    {
        if (level >= GameDefines.miniLevel_Start)
        {
            miniLevels.gameObject.SetActive(true);
        }
    }
}

[Serializable]
public class UIGP_LP_Item
{
    public GameObject CurSign;
    public GameObject FinishSign;
    public Text levelText;
    public GameObject WTip;
    public GameObject Arrow;

    public void SetCur()
    {
        CurSign.SetActive(true);
        FinishSign.SetActive(false);
        if (GameDefines.ifIAA) WTip.SetActive(false);
        else WTip.SetActive(true);
        Arrow.gameObject.SetActive(true);
    }

    public void SetPass()
    {
        CurSign.SetActive(false);
        FinishSign.SetActive(true);
        WTip.SetActive(false);
        Arrow.gameObject.SetActive(false);
    }

    public void SetNotArrived()
    {
        CurSign.SetActive(false);
        FinishSign.SetActive(false);
        if (GameDefines.ifIAA) WTip.SetActive(false);
        else WTip.SetActive(true);
        Arrow.gameObject.SetActive(false);
    }

}