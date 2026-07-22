
using cfg;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UILuckySpin : BaseUI
    {
        private Dictionary<int, ConfLuckySpin> spinData;
        private Dictionary<int, int> weight;
        private Dictionary<int, float> angles;

        protected override void OnAwake()
        {
            spinData = ConfigModule.Instance.Tables.TBLuckySpin.DataMap;

            weight = new Dictionary<int, int>();
            angles = new Dictionary<int, float>();
            int i = 0;
            foreach (KeyValuePair<int, cfg.ConfLuckySpin> conf in spinData)
            {
                int index = i;
                weight.Add(conf.Key, conf.Value.Probability);
                if (index < GameDefines.LS_Angles.Count)
                {
                    angles.Add(conf.Key, GameDefines.LS_Angles[index]);
                    (string name, Sprite sprite) = GetItemInfo((ELuckySpinRewardType)conf.Value.Type);
                    SetItemShow(i, name, sprite);
                }
                i++;
            }
        }

        /// <summary>
        /// 获取名称和图片
        /// </summary>
        /// <param name="type">奖励类型</param>
        /// <returns>名称和图片</returns>
        private (string, Sprite) GetItemInfo(ELuckySpinRewardType type)
        {
            string name = FacadeLanguage.GetText("10100");
            Sprite sprite = null;
            switch (type)
            {
                case ELuckySpinRewardType.Money:
                    name = FacadeLanguage.GetText("10100");
                    sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ifIAA ? GameDefines.ERIAAMoneyIconPath : GameDefines.ERMoneyIconPath);
                    break;
                case ELuckySpinRewardType.Refresh:
                    name = FacadeLanguage.GetText("10027");
                    sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ERProp1IconPath);
                    break;
                case ELuckySpinRewardType.Undo:
                    name = FacadeLanguage.GetText("10029");
                    sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ERProp2IconPath);
                    break;
                case ELuckySpinRewardType.AddBottle:
                    name = FacadeLanguage.GetText("10031");
                    sprite = ResourceMod.Instance.SyncLoad<Sprite>(GameDefines.ERProp3IconPath);
                    break;
            }

            return (name, sprite);
        }

        /// <summary>
        /// 设置某项中的图片和名称
        /// </summary>
        /// <param name="pos">项</param>
        /// <param name="name">名称</param>
        /// <param name="icon">图片</param>
        private void SetItemShow(int pos, string name, Sprite icon)
        {
            switch (pos)
            {
                case 0:
                    mTTItem1name.text = name;
                    mTTItem1Icon.sprite = icon;
                    break;
                case 1:
                    mTTItem2name.text = name;
                    mTTItem2Icon.sprite = icon;
                    break;
                case 2:
                    mTTItem3name.text = name;
                    mTTItem3Icon.sprite = icon;
                    break;
                case 3:
                    mTTItem4name.text = name;
                    mTTItem4Icon.sprite = icon;
                    break;
                case 4:
                    mTTItem5name.text = name;
                    mTTItem5Icon.sprite = icon;
                    break;
                case 5:
                    mTTItem6name.text = name;
                    mTTItem6Icon.sprite = icon;
                    break;
            }
        }

        protected override void OnEnable()
        {
            if (WaterSortWZBridge.HostDriven)
            {
                UIManager.Instance.CloseUI(EUIType.EUILuckySpin);
                return;
            }

            mSpinBg.rotation = Quaternion.identity;
            mBlockMask.gameObject.SetActive(false);

            ShowAnim(mPlane, () =>
            {

            });
        }
        	    private void OnLotteryBtnClickHandle()        {
            RotateTable();        }        /// <summary>
        /// 开转！
        /// </summary>
        private void RotateTable()
        {
            mBlockMask.gameObject.SetActive(true);

            int target = 0;
            target = GetProbability.GatValue<int>(weight);

            ELuckySpinRewardType type = (ELuckySpinRewardType)spinData[target].Type;

            float count = spinData[target].Count;
            float finilAngle = -(GameDefines.LS_rotateCount * 360 - angles[target]);
            //float finilAngle = (GameDefines.LS_rotateCount * 360 + angles[target]);//逆时针
            mSpinBg.transform.DOLocalRotate(Vector3.forward * finilAngle, GameDefines.LS_rotateTime, RotateMode.FastBeyond360).SetEase(Ease.OutQuint).OnComplete(() =>
            {
                STimerManager.Instance.CreateSDelay(0.5f, () =>
                {
                    HideAnim(mPlane, () =>
                    {
                        UIManager.Instance.CloseUI(EUIType.EUILuckySpin);
                        GetReward(type, count);
                    });
                });
            });

            STimerManager.Instance.CreateSTimer(GameDefines.LS_rotateTime - 0.5f, 0, true, true, () =>
            {
                //FacadeAudio.PlayEffect(EAudioType.ECoinCollect);
            });
        }

        /// <summary>
        /// 获取奖励
        /// </summary>
        /// <param name="type">奖励类型</param>
        private void GetReward(ELuckySpinRewardType type, float count)
        {
            switch (type)
            {
                case ELuckySpinRewardType.Money:
                    count = FacadeWithdraw.GetLuckySpinReward(count);
                    FacadePlayer.AddMoney(count);
                    //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                    //{
                    //    new ERewardItemStruct()
                    //    {
                    //        Type = ERewardType.Money,
                    //        Count = count,
                    //    }
                    //}, null);
                    FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                    {
                        Type = ERewardType.Money,
                        Count = count,
                    }, null);
                    break;
                case ELuckySpinRewardType.Refresh:
                    //FacadePlayer.AddProp1Num((int)count);
                    //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                    //{
                    //    new ERewardItemStruct()
                    //    {
                    //        Type = ERewardType.Prop1,
                    //        Count = count,
                    //    }
                    //}, null);
                    FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                    {
                        Type = ERewardType.Prop1,
                        Count = count,
                    }, null);
                    break;
                case ELuckySpinRewardType.Undo:
                    //FacadePlayer.AddProp2Num((int)count);
                    //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                    //{
                    //    new ERewardItemStruct()
                    //    {
                    //        Type = ERewardType.Prop2,
                    //        Count = count,
                    //    }
                    //}, null);
                    FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                    {
                        Type = ERewardType.Prop2,
                        Count = count,
                    }, null);
                    break;
                case ELuckySpinRewardType.AddBottle:
                    //FacadePlayer.AddProp3Num((int)count);
                    //FacadeEffect.PlayGetRewardEffect(new ERewardItemStruct[]
                    //{
                    //    new ERewardItemStruct()
                    //    {
                    //        Type = ERewardType.Prop3,
                    //        Count = count,
                    //    }
                    //}, null);
                    FacadeEffect.PlayGetRewardEffect2(new ERewardItemStruct
                    {
                        Type = ERewardType.Prop3,
                        Count = count,
                    }, null);
                    break;
            }
        }

        private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUILuckySpin);
            });
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}