using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShakeRotateLeftRight : MonoBehaviour
{
    private RectTransform rect;
    public bool ifloop;
    public int shakeCount = 6;
    public float shakeTime = 3;
    public float shakeAngle = 6;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        
        if(ifloop)
            rect.DOPunchRotation(new Vector3(0, 0, shakeAngle), shakeTime, shakeCount, 1).SetLoops(-1);
        else
            rect.DOPunchRotation(new Vector3(0, 0, shakeAngle), shakeTime, shakeCount, 1);
    }

    void OnDisable()
    {
        rect.rotation = Quaternion.identity;
        rect.DOKill();
    }
}
