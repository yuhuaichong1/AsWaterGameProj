using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class AutoCarousel : MonoBehaviour
{
    public int ShowCount;
    public float Height;
    public RectTransform Content;
    public AutoCarouselItem AutoCarouselItem; 
    private Vector2 startP;
    private Vector2 endP;
    private GameObject item;
    private Queue<AutoCarouselItem> childrens;
    private STimer timer;

    private bool inited;

    void Awake()
    {
        AutoCarouselItem.gameObject.SetActive(false);
        inited = false;
    }

    public void Play()
    {
        if (!inited)
        {
            inited = true;

            childrens = new Queue<AutoCarouselItem>();
            startP = Content.anchoredPosition;
            endP = Content.anchoredPosition + new Vector2(0, Height);
            for (int i = 0; i <= ShowCount; i++)
            {
                AutoCarouselItem item = Instantiate(AutoCarouselItem.gameObject, Content).GetComponent<AutoCarouselItem>();
                item.gameObject.SetActive(false);
                item.RectTransform.anchoredPosition = new Vector3(0, -i * Height);
                RandomMsg(item);
                childrens.Enqueue(item);
            }

            timer = STimerManager.Instance.CreateSTimer(4, -1, true, true, () =>
            {
                AutoCarouselItem temp = childrens.Dequeue();
                temp.gameObject.SetActive(true);
                Content.anchoredPosition = startP;
                temp.RectTransform.anchoredPosition -= new Vector2(0, ShowCount * Height);

                foreach (AutoCarouselItem item in childrens)
                {
                    item.RectTransform.anchoredPosition += new Vector2(0, Height);
                }

                childrens.Enqueue(temp);

                RandomMsg(temp);

            }, null, new timingActions()
                {
                    timing = 3f,
                    clockActionType = ClockActionType.After,
                    clockAction = (detalTime)=>
                    {
                        Content.anchoredPosition = Vector2.Lerp(startP, endP, (detalTime - 3f)/ 1);
                    }
                }
            );
        }
        else
        {
            Content.anchoredPosition = startP;

            int i = 0;
            foreach (AutoCarouselItem item in childrens)
            {
                int j = i++;
                item.RectTransform.anchoredPosition = new Vector2(0, -j * Height);
                RandomMsg(item);
            }

            timer.Start();
        }
    }

    public void Stop()
    {
        timer.Stop();
    }

    private void RandomMsg(AutoCarouselItem obj)
    {
        List<PayNode> payItems = FacadePayType.GetPayItems();

        int count = payItems.Count;
        obj.transform.GetChild(0).GetComponent<Image>().sprite = payItems[UnityEngine.Random.Range(0, count)].icon;

        string name = FacadePlayer.GetRandomName();
        int level = UnityEngine.Random.Range(1, 10);
        int times = UnityEngine.Random.Range(1, 50);
        float money = times * UnityEngine.Random.Range(8f, 12f);
        string moneyShow = FacadePayType.RegionalChange(money);

        obj.transform.GetChild(1).GetComponent<Text>().text = string.Format(FacadeLanguage.GetText("10006"), name, moneyShow);
    }
}
