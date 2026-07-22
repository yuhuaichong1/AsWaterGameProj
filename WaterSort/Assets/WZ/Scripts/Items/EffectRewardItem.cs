using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class EffectRewardItem : MonoBehaviour
    {
        public Image Icon;//奖励图片
        public Text Desc;//奖励介绍
        [HideInInspector]
        public ERewardType ErType;//奖励类型
        [HideInInspector]
        public float Count; //奖励数量

        /// <summary>
        /// 展示奖励项每日
        /// </summary>
        /// <param name="erType">奖励图片</param>
        /// <param name="count">奖励数量</param>
        public void Show(ERewardType erType, float count)
        {
            ErType = erType;
            Count = count;
            EnsureIconRef();

            switch (erType)
            {
                case ERewardType.Money:
                    ApplyIcon(ResolveMoneyStackSprite());
                    if (Desc != null)
                        Desc.text = FacadePayTypeExtend.RegionalChangeHandle(Count);
                    break;
                case ERewardType.Gem:
                    ApplyIcon(FacadePayTypeExtend.GetMulteGem?.Invoke());
                    if (Desc != null)
                        Desc.text = FacadePayTypeExtend.RegionalChangeHandle(Count);
                    break;
                case ERewardType.AddBottle:
                    ApplyIcon(Resources.Load<Sprite>(GameDefines.ERAddSpaceIconPath));
                    if (Desc != null)
                        Desc.text = Count.ToString();
                    break;
                case ERewardType.Clear:
                    ApplyIcon(Resources.Load<Sprite>(GameDefines.ERClearIconPath));
                    if (Desc != null)
                        Desc.text = Count.ToString();
                    break;
                case ERewardType.Undo:
                    ApplyIcon(Resources.Load<Sprite>(GameDefines.ERHammerIconPath));
                    if (Desc != null)
                        Desc.text = Count.ToString();
                    break;

                case ERewardType.Prompt:
                    ApplyIcon(Resources.Load<Sprite>(GameDefines.ERAddPromptIconPath));
                    if (Desc != null)
                        Desc.text = Count.ToString();
                    break;
                case ERewardType.Guid:
                    ApplyIcon(Resources.Load<Sprite>(GameDefines.ERAddGuidIconPath));
                    if (Desc != null)
                        Desc.text = Count.ToString();
                    break;
                case ERewardType.LuckyWalletMoney:
                    ApplyIcon(Resources.Load<Sprite>(GameDefines.LSLuckyWalletIconPath));
                    if (Desc != null)
                        Desc.text = FacadePayTypeExtend.RegionalChangeHandle(Count);
                    break;
            }
        }

        private void EnsureIconRef()
        {
            if (Icon != null)
                return;

            Transform iconTf = transform.Find("Icon");
            if (iconTf != null)
                Icon = iconTf.GetComponent<Image>();
            if (Icon == null)
                Icon = GetComponentInChildren<Image>(true);
        }

        private void ApplyIcon(Sprite sprite)
        {
            if (Icon == null)
                return;
            // 加载失败时保留预制体上已有的图，避免被刷成 None。
            if (sprite != null)
                Icon.sprite = sprite;
        }

        private static Sprite ResolveMoneyStackSprite()
        {
            if (GameDefines.ifIAA)
                return Resources.Load<Sprite>(GameDefines.ERIAAMoneyIconPath);

            Sprite sprite = FacadePayTypeExtend.GetMultCoinCoinAct?.Invoke();
            if (sprite != null)
                return sprite;

            return Resources.Load<Sprite>(GameDefines.ERMultCoinIconPath);
        }
    }
}
