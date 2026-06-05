using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIGuide : BaseUI
    {
        private TDAnalyticsManager TDAnalyticsManager;

        private List<Transform> diglogs;
        private List<Transform> hands;
        private List<Transform> transparents;
        private List<Transform> clicks;
        private Dictionary<Transform, (Transform, int)> preOnMaskObj;

        private float handCorrection;

        protected override void OnAwake()
        {
            TDAnalyticsManager = ModuleMgr.Instance.TDAnalyticsManager;

            FacadeAdd();

            mGuidePlane.gameObject.SetActive(false);
            diglogs = new List<Transform>();
            mGuideText.gameObject.SetActive(false);
            hands = new List<Transform>();
            mHander.gameObject.SetActive(false);
            transparents = new List<Transform>();
            mHole.gameObject.SetActive(false);
            clicks = new List<Transform>();
            preOnMaskObj = new Dictionary<Transform, (Transform, int)>();
        }
        protected override void OnEnable()
        {

        }

        #region Facade

        private void FacadeAdd()
        {
            FacadeGuide.PlayGuide += PlayGuide;
            FacadeGuide.CloseGuide += CloseGuide;
            FacadeGuide.RestorePreOnMaskObjs += RestorePreOnMaskObjs;
            FacadeGuide.SetHandCorrection += SetHandCorrection;

        }

        private void FacadeRemove()
        {
            FacadeGuide.PlayGuide -= PlayGuide;
            FacadeGuide.CloseGuide -= CloseGuide;
            FacadeGuide.RestorePreOnMaskObjs -= RestorePreOnMaskObjs;
            FacadeGuide.SetHandCorrection -= SetHandCorrection;

        }

        #endregion

        /// <summary>
        /// 设置当前引导
        /// </summary>
        private void SetGuideShow()
        {
            GuideItem info = FacadeGuide.GetCurGuideItems();
            bool ifshow;

            if (info.extraStart != null && info.extraStart.Count != 0)
                SetExtraStart(info.extraStart);

            ifshow = info.diglogPos != null;
            mGuideTextFather.gameObject.SetActive(ifshow);
            if (ifshow)
            {
                SetDialog(FindTrans(info.diglogPos), info.diglogContent);
            }

            ifshow = info.handPos != null;
            mHandFather.gameObject.SetActive(ifshow);
            if (ifshow)
            {
                SetHander(FindTrans(info.handPos));
            }

            mHoleMask.alpha = info.ifMask ? 1 : 0;

            ifshow = info.transparentPos != null;
            if (ifshow)
            {
                SetHole(FindTrans(info.transparentPos));
            }

            float autoHiddenTime = info.autohiddenTime;
            if (autoHiddenTime != 0)
            {
                STimerManager.Instance.CreateSDelay(autoHiddenTime, () =>
                {
                    FacadeGuide.NextStep();
                });
            }
            else
            {
                ifshow = info.clickPos != null;
                mMask.penetrateObjs.Clear();
                if (ifshow)
                {
                    List<Transform> transPath = FindTrans(info.clickPos);
                    foreach (Transform tran in transPath)
                    {
                        mMask.penetrateObjs.Add(tran.GetComponent<RectTransform>());
                    }

                    mMask.ifNext = info.ifNextPlay;
                }
            }



            ifshow = info.onMaskObjs != null;
            if (ifshow)
            {
                SetOnMaskObjs(FindTrans(info.onMaskObjs));
            }
        }

        /// <summary>
        /// 设置提示框
        /// </summary>
        /// <param name="newTarget">新的目标</param>
        /// <param name="newContent">新的内容</param>
        private void SetDialog(List<Transform> newTarget, List<string> newContent)
        {
            for (int i = 0; i < newTarget.Count; i++)
            {
                Transform tempTran;
                if (i < diglogs.Count)
                {
                    tempTran = diglogs[i];
                    tempTran.gameObject.SetActive(true);
                }
                else
                {
                    GameObject obj = GameObject.Instantiate(mGuideText.gameObject, mGuideTextFather);
                    obj.gameObject.SetActive(true);
                    tempTran = obj.GetComponent<Transform>();
                    diglogs.Add(tempTran);
                }

                tempTran.position = newTarget[i].position;
                tempTran.GetComponent<RectTransform>().sizeDelta = newTarget[i].GetComponent<RectTransform>().sizeDelta;
                tempTran.GetChild(1).GetComponent<Text>().text = newContent[i];
            }
        }

        /// <summary>
        /// 设置手位置
        /// </summary>
        /// <param name="newTarget"></param>
        private void SetHander(List<Transform> newTarget)
        {
            for (int i = 0; i < newTarget.Count; i++)
            {
                Transform tempTran;
                if (i < hands.Count)
                {
                    tempTran = hands[i];
                    tempTran.gameObject.SetActive(true);
                }
                else
                {
                    GameObject obj = GameObject.Instantiate(mHander.gameObject, mHandFather);
                    obj.gameObject.SetActive(true);
                    tempTran = obj.GetComponent<Transform>();
                    hands.Add(tempTran);
                }

                tempTran.position = newTarget[i].position + new Vector3(0, handCorrection, 0);
                tempTran.GetComponent<RectTransform>().sizeDelta = newTarget[i].GetComponent<RectTransform>().sizeDelta;
                tempTran.GetChild(0).GetComponent<AutoHandSwing>().Reset();
            }
        }

        private void SetHole(List<Transform> newTarget)
        {
            for (int i = 0; i < newTarget.Count; i++)
            {
                Transform tempTran;
                if (i < transparents.Count)
                {
                    tempTran = transparents[i];
                    tempTran.gameObject.SetActive(true);
                }
                else
                {
                    GameObject obj = GameObject.Instantiate(mHole.gameObject, mHoleFather);
                    obj.gameObject.SetActive(true);
                    tempTran = obj.GetComponent<Transform>();
                    transparents.Add(tempTran);
                }

                tempTran.position = newTarget[i].position;
                tempTran.GetComponent<RectTransform>().sizeDelta = newTarget[i].GetComponent<RectTransform>().sizeDelta;
            }
        }

        private void RestorePreOnMaskObjs()
        {
            if (preOnMaskObj == null) return;

            foreach (var kvp in preOnMaskObj.ToList())
            {
                Transform objTrans = kvp.Key;
                var (originalParent, originalIndex) = kvp.Value;

                if (objTrans != null && originalParent != null)
                {
                    objTrans.parent = originalParent;
                    objTrans.SetSiblingIndex(originalIndex);
                }
            }
            preOnMaskObj.Clear();
        }

        private void SetOnMaskObjs(List<Transform> onMaskObjs)
        {
            preOnMaskObj.Clear();

            foreach (Transform objTrans in onMaskObjs)
            {
                if (objTrans == null) continue;

                preOnMaskObj.TryAdd(objTrans, (objTrans.parent, objTrans.GetSiblingIndex()));

                objTrans.parent = mOnMaskObjs;
                objTrans.SetSiblingIndex(mOnMaskObjs.childCount - 1);
            }
        }

        /// <summary>
        /// 激活引导开始时的回调
        /// </summary>
        /// <param name="extraData">回调数据类型</param>
        private void SetExtraStart(Dictionary<string, string> extraData)
        {
            foreach (KeyValuePair<string, string> kvp in extraData)
            {
                switch (kvp.Key)
                {
                    case "handC":
                        handCorrection = bool.Parse(kvp.Value) ? -0.5f : 0;
                        break;
                }
            }
        }



        private void PlayGuide()
        {
            if (!FacadeGuide.GetIfTutorial())
                return;

            //TDAnalyticsManager.GuideStep(FacadeGuide.GetCurStep());

            mGuidePlane.gameObject.SetActive(true);

            foreach (Transform objTrans in diglogs)
            {
                objTrans.gameObject.SetActive(false);
            }
            foreach (Transform objTrans in hands)
            {
                objTrans.gameObject.SetActive(false);
            }
            foreach (Transform objTrans in transparents)
            {
                objTrans.gameObject.SetActive(false);
            }
            clicks.Clear();

            SetGuideShow();
        }

        private void CloseGuide()
        {
            mGuidePlane.gameObject.SetActive(false);
        }

        private List<Transform> FindTrans(List<string> paths)
        {
            List<Transform> trans = new List<Transform>();
            foreach (string path in paths)
            {
                trans.Add(GameObject.Find(path).transform);
            }

            return trans;
        }

        private void SetHandCorrection(int value)
        {
            handCorrection = value;
        }

        protected override void OnDisable() { }
        protected override void OnDispose()
        {
            TDAnalyticsManager = null;

            FacadeRemove();

            diglogs = null;
            hands = null;
            transparents = null;
            clicks = null;
        }
    }
}