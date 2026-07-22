using UnityEngine;
using UnityEngine.UI;
using System.Collections;
namespace WZSDK
{
    public class TextMarquee : MonoBehaviour
    {

        public float scrollDuration = 10f;
        public float startX = 1500f;
        public float resetOffset = -1600f;

        private RectTransform textRect;
        public Text Marque1;
        public Image icon1;
        public Text Marque2;
        public Image icon2;

        private bool isPaused = false;
        private Vector2 pausedPosition;
        private Coroutine scrollCoroutine;


        public System.Action onScrollComplete;

        void Awake()
        {
            textRect = GetComponent<RectTransform>();

        }

        void OnEnable()
        {

        }

        void OnDisable()
        {
            StopScrolling();
        }

        public void SetMarqueeText(string barrage1, string barrage2)
        {
            Sprite[] paymentIcons = new Sprite[3];
            paymentIcons[0] = Resources.Load<Sprite>("UI/Marquee/paypay_s");
            paymentIcons[1] = Resources.Load<Sprite>("UI/Marquee/venmo_s");
            paymentIcons[2] = Resources.Load<Sprite>("UI/Marquee/zelle_s");

            Marque1.text = barrage1;
            Marque2.text = barrage2;

            int firstIndex = Random.Range(0, paymentIcons.Length);
            int secondIndex;
            do
            {
                secondIndex = Random.Range(0, paymentIcons.Length);
            } while (secondIndex == firstIndex && paymentIcons.Length > 1);

            icon1.sprite = paymentIcons[firstIndex];
            icon2.sprite = paymentIcons[secondIndex];

            ResetPosition();
            StartScrolling();
        }

        private void ResetPosition()
        {
            textRect.anchoredPosition = new Vector2(startX, textRect.anchoredPosition.y);
        }

        private void StartScrolling()
        {
            StopScrolling();
            scrollCoroutine = GameManagerWZ.instance.StartCoroutine(ScrollCoroutine());
        }

        private void StopScrolling()
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
        }

        private IEnumerator ScrollCoroutine()
        {
            float elapsedTime = 0f;
            Vector2 startPos = new Vector2(startX, textRect.anchoredPosition.y);
            Vector2 endPos = new Vector2(resetOffset, textRect.anchoredPosition.y);

            while (elapsedTime < scrollDuration)
            {
                if (!isPaused)
                {
                    float t = elapsedTime / scrollDuration;
                    textRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    elapsedTime += Time.deltaTime;
                }
                else
                {
                    pausedPosition = textRect.anchoredPosition;
                }
                yield return null;
            }
            gameObject.SetActive(false);
            onScrollComplete?.Invoke();
        }

        public void Pause()
        {
            if (!isPaused)
            {
                isPaused = true;
                pausedPosition = textRect.anchoredPosition;
            }
        }

        public void Resume()
        {
            if (isPaused)
            {
                isPaused = false;
                textRect.anchoredPosition = pausedPosition;
            }
        }

        public bool IsPaused()
        {
            return isPaused;
        }
    }
}