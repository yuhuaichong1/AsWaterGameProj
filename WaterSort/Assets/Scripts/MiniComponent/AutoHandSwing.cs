using DG.Tweening;
using UnityEngine;

public class AutoHandSwing : MonoBehaviour
{

    public bool ifAwake = true;//是否在一开始就启用
    private RectTransform rect;
    private Vector3 scaleVec3;


    void Awake()
    {
        scaleVec3 = new Vector3(1.25f, 1.25f, 1.25f);
        rect = this.GetComponent<RectTransform>();

        if (ifAwake)
        {
            StartSwing();
        }
    }

    public void StartSwing()
    {
        //rect.localScale = Vector3.one;
        Sequence sequence = DOTween.Sequence();
        sequence.AppendCallback(() => { rect.localScale = Vector3.one; });
        sequence.Append(rect.DOScale(scaleVec3, 0.6f));
        sequence.Append(rect.DOScale(Vector3.one, 0.6f));
        sequence.SetLoops(-1);
        sequence.Play();
    }

    public void Reset()
    {

    }
}
