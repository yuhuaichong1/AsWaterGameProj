using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WZSDK
{
    public class BaseViewWithDraw : UIManagedViewBase
    {
        protected Dictionary<string, GameObject> m_cache_gos = new Dictionary<string, GameObject>();
        protected bool isShow;

        void Awake()
        {
            OnAwake();
        }

        void Start()
        {
            OnStart();
        }

        protected virtual void OnAwake()
        {
        }

        protected virtual void OnStart()
        {
        }

        public override bool IsShow()
        {
            return isShow;
        }

        public override void InitUi()
        {
        }

        public override void InitData()
        {
            MarkInitialized();
        }

        public virtual void InitView()
        {
        }

        protected virtual void HandleViewArgs(object[] args)
        {
        }

        public virtual void ShowView()
        {
            ShowView(null);
        }

        public virtual void ShowView(params object[] args)
        {
            gameObject.SetActive(true);
            isShow = true;

            if (args != null && args.Length > 0)
            {
                HandleViewArgs(args);
            }

            MarkInitialized();
            InitView();
        }

        public virtual void HideView()
        {
            isShow = false;
            gameObject.SetActive(false);
        }

        public override void Open(params object[] args)
        {
            ShowView(args);
        }

        public override void Close(params object[] args)
        {
            HideView();
            DestroyView();
        }

        public override void SetVisible(bool value)
        {
            gameObject.SetActive(value);
            isShow = value;
        }

        public GameObject Find(string res)
        {
            if (m_cache_gos.ContainsKey(res))
            {
                return m_cache_gos[res];
            }

            m_cache_gos.Add(res, transform.Find(res).gameObject);
            return m_cache_gos[res];
        }

        public T Find<T>(string res) where T : Component
        {
            GameObject obj = Find(res);
            return obj.GetComponent<T>();
        }
    }
}
