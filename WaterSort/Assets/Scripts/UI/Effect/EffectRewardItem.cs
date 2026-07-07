using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class EffectRewardItem : MonoBehaviour
{
    public Image Icon;//奖励图片
    public Text Desc;//奖励介绍
    [HideInInspector]
    public ERewardType ErType;//奖励类型
    [HideInInspector]
    public double Count; //奖励数量

    /// <summary>
    /// 展示奖励项每日
    /// </summary>
    /// <param name="erType">奖励图片</param>
    /// <param name="count">奖励数量</param>
    public void Show(ERewardType erType, double count)
    {
        ErType = erType;
        Count = count;

        switch (erType) 
        {
            case ERewardType.Money:
                Icon.sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ifIAA? GameDefines.ERIAAMoneyIconPath : GameDefines.ERMoneyIconPath);
                Icon.rectTransform.sizeDelta = new Vector2(164, 128);
                Desc.text = FacadePayType.RegionalChange(Count);
                break;
            case ERewardType.Prop1:
                Icon.sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ERProp1IconPath);
                Icon.SetNativeSize();
                Desc.text = Count.ToString();
                break;
            case ERewardType.Prop2:
                Icon.sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ERProp2IconPath);
                Icon.SetNativeSize();
                Desc.text = Count.ToString();
                break;
            case ERewardType.Prop3:
                Icon.sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ERProp3IconPath);
                Icon.SetNativeSize();
                Desc.text = Count.ToString();
                break;
        }
    }

}
