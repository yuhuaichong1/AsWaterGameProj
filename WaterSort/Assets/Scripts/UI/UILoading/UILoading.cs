using System;
using System.Resources;
using UnityEngine;
using UnityEngine.UI;
namespace XrCode
{
    public partial class UILoading : BaseUI
    {
        private STimer loadingTextAnimTimer;
        private float LTATime = 0.33f;
        private int curLTATextId = 1;

        private float speed = 0.35f;
        private STimer startLoadingTimer;
        private float curSliderMoveValue;
        private float targetValue;
        private float time;

        protected override void OnAwake()
        {
            ModuleMgr.Instance.SceneMod.LoadingValue += ChangeValue;
            ModuleMgr.Instance.SceneMod.OnLoadChanged += ChangeValue2;
            FacadeGamePlay.LoadingSilderMoveAnim += LoadingSilderMoveAnim;
        }

        // 进度条加载完毕之后，开始进入游戏/开始进入游戏主逻辑
        // 当游戏前期内容加载结束，进度条相应反馈

        private void LoadingSilderMoveAnim()
        {
            float curSliderValue = mLoadingSlider.value;
            targetValue = (float)Game.Instance.curPreLoadCount / Game.Instance.maxPreLoadCount;
            time = (targetValue - curSliderValue) / speed;
            curSliderMoveValue = targetValue - curSliderValue;

            if (startLoadingTimer == null)
            {
                startLoadingTimer = STimerManager.Instance.CreateSTimer(time, 0, true, false, () => 
                {
                    if (targetValue == 1)
                    {
                        mLoadingSlider.value = 1;
                        ModuleMgr.Instance.Start();
                        Game.Instance.GameState = EGameState.Run;
                    }
                }, (secd) =>
                {
                    mLoadingSlider.value = curSliderValue + (secd / time) * curSliderMoveValue;
                });
            }
            else
            {
                startLoadingTimer.targetTime = time;
                startLoadingTimer.ReStart();
            }
        }

        private void SetLoadingTextAnim(bool b = true)
        {
            if(loadingTextAnimTimer == null)
            {
                if(!b)
                {
                    return;
                }

                loadingTextAnimTimer = STimerManager.Instance.CreateSTimer(LTATime, -1, false, true, () => 
                {
                    switch(curLTATextId)
                    {
                        case 1:
                            curLTATextId++;
                            mLoadingText.text = "Loading.";
                            break;
                        case 2:
                            curLTATextId++;
                            mLoadingText.text = "Loading..";
                            break;
                        case 3:
                            curLTATextId = 1;
                            mLoadingText.text = "Loading...";
                            break;
                    }
                });
            }
            else
            {
                if (!b)
                {
                    loadingTextAnimTimer.Stop();
                }
                else
                {
                    loadingTextAnimTimer.ReStart();
                }
            }
        }

        protected override void OnEnable() 
        {
            //mGameTitle.sprite = ResourceMod.Instance.SyncLoad<Sprite>($"UI/Logo/{FacadePayType.GetLanguage()}_Logo.png");
            mGameTitle.SetNativeSize();
            SetLoadingTextAnim();
        }

        void ChangeValue2(string str)
        {
            Debug.Log("当前加载的场景名称：：" + str);
        }


        private float needtime;
        private bool canload;
        public void SilderMove()
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            needtime = UnityEngine.Random.Range(1, 6);
            mLoadingSlider.value = 0;
            canload = true;
        }

        public void ChangeValue(float asynvalue)
        {
            mLoadingSlider.value = asynvalue;
            mLoadingSche.text = Mathf.RoundToInt(asynvalue).ToString() + "%";
        }


        protected override void OnUpdate()
        {
            /*
            if(canload)
            {
                mLoadingSche.text = Mathf.RoundToInt(mLoadingSlider.value).ToString() + "%";
                mLoadingSlider.value += Time.deltaTime * needtime;
            }
            */
        }

        protected override void OnDisable() { }
        protected override void OnDispose()
        {
            ModuleMgr.Instance.SceneMod.LoadingValue -= ChangeValue;
            ModuleMgr.Instance.SceneMod.OnLoadChanged -= ChangeValue2;
            FacadeGamePlay.LoadingSilderMoveAnim -= LoadingSilderMoveAnim;

            SetLoadingTextAnim(false);
        }
    }
}