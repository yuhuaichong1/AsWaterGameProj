using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WZSDK
{
    public interface IBaseView
    {
        bool IsInit();
        bool IsShow();
        void InitUi();
        void InitData();
        void Open(params object[] args);
        void Close(params object[] args);
        void DestroyView();
        void SetVisible(bool value);
        int ViewId { get; set; }
    }

    public abstract class UIManagedViewBase : MonoBehaviour, IBaseView
    {
        private bool _isInit;

        public int ViewId { get; set; }

        public virtual bool IsInit()
        {
            return _isInit;
        }

        public abstract bool IsShow();

        public virtual void InitUi()
        {
        }

        public virtual void InitData()
        {
            MarkInitialized();
        }

        public abstract void Open(params object[] args);

        public abstract void Close(params object[] args);

        public virtual void DestroyView()
        {
            Destroy(gameObject);
        }

        public virtual void SetVisible(bool value)
        {
            gameObject.SetActive(value);
        }

        protected void MarkInitialized()
        {
            _isInit = true;
        }
    }
}
