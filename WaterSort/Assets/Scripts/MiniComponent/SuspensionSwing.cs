using DG.Tweening;
using UnityEngine;

public class SuspensionSwing : MonoBehaviour
{
    public float SuspTime = 2;
    public float SuspDist = 50f;
    public float SwingTime = 2;
    public float SwingAngle = 45f;

    private Vector3 SuspOrginPos;

    private void Awake()
    {
        SuspOrginPos = transform.localPosition;
    }

    void Start()
    {

        StartMove();
    }

    public void StartMove()
    {
        transform.DOLocalMoveY(SuspOrginPos.y + SuspDist, SuspTime).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuad);
        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DORotate(new Vector3(0, 0, SwingAngle), SwingTime / 4).SetEase(Ease.InSine));
        sequence.Append(transform.DORotate(new Vector3(0, 0, -SwingAngle), SwingTime / 2).SetEase(Ease.InOutQuad));
        sequence.Append(transform.DORotate(new Vector3(0, 0, 0), SwingTime / 4).SetEase(Ease.InSine));
        sequence.SetLoops(-1, LoopType.Restart);
    }
    
    void Update()
    {
        
    }
}
