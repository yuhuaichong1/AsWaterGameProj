using UnityEngine;
using UnityEngine.UI;

public class UILoadingWaiting : MonoBehaviour
{
    public GameObject ULWobj;
    public Text text;

    private float delayTime;
    //[TextArea]
    private string waitTextContent;
    private STimer showTimer;
    private STimer textTimer;
    private string[] showText;
    private int i;

    private bool ifInit;

    private void Awake()
    {
        //Init();
    }

    private void Init()
    {
        delayTime = GameDefines.loadwait_delay;
        waitTextContent = GameDefines.loadwait_content;

        showText = new string[4]
        {
            $"{waitTextContent}",
            $"{waitTextContent}.",
            $"{waitTextContent}..",
            $"{waitTextContent}...",
        };
        i = 0;
        text.text = showText[0];
    }

    public void StartTextAnim()
    {
        return;

        if(!ifInit)
        {
            ifInit = true;
            Init();
        }

        if (showTimer == null)
        {
            showTimer = STimerManager.Instance.CreateSTimer(delayTime, 0, true, false, () => 
            {
                ULWobj.gameObject.SetActive(true);
                if (textTimer == null)
                {
                    textTimer = STimerManager.Instance.CreateSTimer(0.5f, -1, true, false, () =>
                    {
                        text.text = showText[i];
                        if (i == showText.Length - 1)
                            i = 0;
                        else
                            i++;
                    });
                }
                else
                {
                    i = 0;
                    textTimer.ReStart();
                }
            });
        }
        else
        {
            showTimer.ReStart();
        }
    }

    public void StopTextAnim()
    {
        if(showTimer != null)
        {
            showTimer.Stop();
        }
        if (textTimer != null)
        {
            textTimer.Stop();
        }

        ULWobj.gameObject.SetActive(false);
    }
}
