using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WZSDK
{
public class PropView : BaseView
{
    [Header("UI Elements")]
    [SerializeField] private Image PropIcon;
    [SerializeField] private Button AdBtn;
    [SerializeField] private Button ExitBtn;
    [SerializeField] private Text Title;
    [SerializeField] private Text Dec;

    [Header("Prop Type")]
    [SerializeField] private int propType;

    private bool _isEventsBound;
    private GameManagerWZ GM => GameManagerWZ.instance;

    public override void InitView()
    {
        RefreshPropInfo();
        BindEvents();
    }

    protected override void HandleViewArgs(object[] args)
    {
        if (args != null && args.Length > 0)
        {
            if (args[0] is int type)
            {
                SetPropType(type);
            }
        }
    }

    public void SetPropType(int type)
    {
        propType = type;
        RefreshPropInfo();
    }

    private void RefreshPropInfo()
    {
        if (!GM) return;

        switch (propType)
        {
           case 1: // 添加瓶子
                  SetPropDisplay(
                      iconPath: GameDefines.ERAddSpaceIconPath,
                      titleKey: "1013",
                      descKey: "1012",
                      count: GM.currentAddBottleCount
                  );
                  break;

              case 2: // 清除
                  SetPropDisplay(
                      iconPath: GameDefines.ERClearIconPath,
                      titleKey: "1015",
                      descKey: "1014",
                      count: GM.currentClear
                  );
                  break;

              case 3: // 撤销
                  SetPropDisplay(
                      iconPath: GameDefines.ERHammerIconPath,
                      titleKey: "1017",
                      descKey: "1016",
                      count: GM.currentUndo
                  );
                  break;
                /*     case 4: // 提示
                      SetPropDisplay(
                          iconPath: GameDefines.ERAddPromptIconPath,
                          titleKey: "2146", 
                          descKey: "2147", 
                          count: GameData.Hint.Value
                      );
                      break;
                  case 5: //线路 
                      SetPropDisplay(
                          iconPath: GameDefines.ERAddGuidIconPath,
                          titleKey: "2144",
                          descKey: "2145",
                          count: GameData.GridLines.Value
                      );
                      break;*/
        }
    }

    private void SetPropDisplay(string iconPath, string titleKey, string descKey, int count)
    {
        if (PropIcon != null && !string.IsNullOrEmpty(iconPath))
        {
            Sprite iconSprite = Resources.Load<Sprite>(iconPath);
            if (iconSprite != null)
            {
                PropIcon.sprite = iconSprite;
                PropIcon.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError($"Failed to load icon from path: {iconPath}");
            }
        }

        if (Title != null)
            Title.text = LocalizationManager.Instance.GetText(titleKey);

        if (Dec != null)
            Dec.text = LocalizationManager.Instance.GetText(descKey);
    }

    private void BindEvents()
    {
        if (_isEventsBound) return;

        if (AdBtn != null)
            AdBtn.onClick.AddListener(OnAdBtnClick);

        if (ExitBtn != null)
            ExitBtn.onClick.AddListener(OnExitBtnClick);

        _isEventsBound = true;
    }

    private void RemoveEvents()
    {
        if (AdBtn != null)
            AdBtn.onClick.RemoveAllListeners();

        if (ExitBtn != null)
            ExitBtn.onClick.RemoveAllListeners();

        _isEventsBound = false;
    }

    private void PlayClick()
    {

    }

    private void OnExitBtnClick()
    {
        PlayClick();
        UIManager.instance.CloseView(EUIType.PropView);
    }

    private void OnAdBtnClick()
    {
        PlayClick();
        if (!GM) return;

        string adPlacement = "";
        System.Action<bool> rewardCallback = null;

        switch (propType)
        {
            case 1: // 添加瓶子
                adPlacement = "add_bottle_prop";
                rewardCallback = success =>
                {
                    if (success)
                    {
                        UIManager.instance.CloseView(EUIType.PropView);
                        PlayRewardEffect(ERewardType.AddBottle, 1);
                    }
                };
                break;

            case 2: // 清除
                adPlacement = "clear_prop";
                rewardCallback = success =>
                {
                    if (success)
                    {
                        UIManager.instance.CloseView(EUIType.PropView);
                        PlayRewardEffect(ERewardType.Clear, 1);
                    }
                };
                break;

            case 3: // 撤销
                adPlacement = "undo_prop";
                rewardCallback = success =>
                {
                    if (success)
                    {
                        UIManager.instance.CloseView(EUIType.PropView);
                        PlayRewardEffect(ERewardType.Undo, 1);
                    }
                };
                break;
            case 4: // 提示
                adPlacement = "prompt_prop";
                rewardCallback = success =>
                {
                    if (success)
                    {
                        UIManager.instance.CloseView(EUIType.PropView);
                        PlayRewardEffect(ERewardType.Prompt, 1);
                    }
                };
                break;
            case 5: // 路线
                adPlacement = "guid_prop";
                rewardCallback = success =>
                {
                    if (success)
                    {
                        UIManager.instance.CloseView(EUIType.PropView);
                        PlayRewardEffect(ERewardType.Guid, 1);
                    }
                };
                break;
        }

        AdsControl.Instance.ShowRewardedAd(adPlacement, rewardCallback);
    }

    private void PlayRewardEffect(ERewardType rewardType, float count)
    {
        if (FacadeEffectExtend.PlayGetRewardEffectHandle == null)
        {
            ApplyRewardDirectly(rewardType, count);
            return;
        }

        FacadeEffectExtend.PlayGetRewardEffectHandle(new ERewardItemStruct[]
        {
            new ERewardItemStruct()
            {
                Type = rewardType,
                Count = count,
            }
        }, null);
    }

    private void ApplyRewardDirectly(ERewardType rewardType, float count)
    {
        int rewardCount = Mathf.RoundToInt(count);
        switch (rewardType)
        {
            case ERewardType.AddBottle:
                GM?.RewardHintBottle(rewardCount);
                break;
            case ERewardType.Clear:
                GM?.RewardClear(rewardCount);
                break;
            case ERewardType.Undo:
                GM?.RewardUndo(rewardCount);
                break;
        }
    }

    private void ShowRewardFeedback(string message)
    {
        UIManager.instance.CloseView(EUIType.PropView);
    }

    public override void ShowView()
    {
        base.ShowView();
        RefreshPropInfo();
        BindEvents();
    }

    public override void Start() { }

    public override void Update() { }

    private void OnDestroy()
    {
        if (_isEventsBound) RemoveEvents();
    }
}
}
