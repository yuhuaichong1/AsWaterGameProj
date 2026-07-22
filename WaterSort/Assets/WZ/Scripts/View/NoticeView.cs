using UnityEngine;
using UnityEngine.UI;
using System.Collections;
namespace WZSDK
{
    public class NoticeView : BaseView
    {
        public Text noticeText;
        public float duration = 2f;

        private Coroutine _hideCoroutine;

        public override void InitView() { }
        public override void Start() { }
        public override void Update() { }

        protected override void HandleViewArgs(object[] args)
        {
            if (args != null && args.Length > 0 && args[0] != null)
            {
                string info = args[0].ToString();
                noticeText.text = info;


                if (_hideCoroutine != null)
                {
                    StopCoroutine(_hideCoroutine);
                    _hideCoroutine = null;
                }


                _hideCoroutine = StartCoroutine(HideAfterDelay(duration));
            }
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);


            if (this != null && gameObject != null)
            {
                UIManager.instance.CloseView(EUIType.NoticeView);
            }

            _hideCoroutine = null;
        }

        private void OnDisable()
        {

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
        }
    }
}