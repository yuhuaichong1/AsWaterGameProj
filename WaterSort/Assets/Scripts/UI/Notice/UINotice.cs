using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UINotice : BaseUI
    {
        private GameObject NItem;
        private GameObject NItem2;

        private Stack<GameObject> NItems;
        private Stack<GameObject> NItem2s;

        protected override void OnAwake() 
        {
            NItem = ResourceMod.Instance.SyncLoad<GameObject>("Prefabs/UI/Notice/NoticeItem.prefab");
            NItems = new Stack<GameObject>();

            NItem2 = ResourceMod.Instance.SyncLoad<GameObject>("Prefabs/UI/Notice/NoticeItem2.prefab");
            NItem2s = new Stack<GameObject>();
        }

        protected override void OnEnable()
        {
            mGameObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }

        public GameObject ShowInfo(string info)
        {
            GameObject temp;
            if (NItems.Count != 0)
            {
                temp = NItems.Pop();
                temp.SetActive(true);
            }
            else
            {
                temp = GameObject.Instantiate(NItem, mTransform);
            }

            temp.transform.position = mStartPos.position;
            temp.transform.GetChild(0).GetComponent<Text>().text = info;

            temp.transform.localScale = Vector3.zero;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(temp.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
            sequence.AppendInterval(1);
            sequence.Append(temp.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.Linear));
            sequence.Play().OnComplete(() =>
            {
                temp.SetActive(false);
                NItems.Push(temp);
            });

            return temp;
        }

        public GameObject ShowInfo2(string info, float time = 2)
        {
            GameObject temp;
            if (NItems.Count != 0)
            {
                temp = NItem2s.Pop();
                temp.SetActive(true);
            }
            else
            {
                temp = GameObject.Instantiate(NItem2, mTransform);
            }

            temp.transform.position = mStartPos2.position;
            temp.transform.GetChild(0).GetComponent<Text>().text = info;

            temp.transform.localScale = Vector3.zero;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(temp.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
            sequence.AppendInterval(1);
            sequence.Append(temp.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.Linear));
            sequence.Play().OnComplete(() =>
            {
                temp.SetActive(false);
                NItem2s.Push(temp);
            });

            return temp;
        }

        public void HideInfo(GameObject obj)
        {
            obj.SetActive(false);
            NItems.Push(obj);
        }

        protected override void OnDisable()
        {
        
        }
        protected override void OnDispose()
        {
        
        }
    }
}