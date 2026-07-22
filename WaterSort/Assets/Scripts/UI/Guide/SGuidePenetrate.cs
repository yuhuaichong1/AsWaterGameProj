using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SGuidePenetrate : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image mask;
    public List<RectTransform> penetrateObjs;
    public bool ifNext;

    void Awake()
    {
        // Keep prefab-serialized penetrate targets (WZ Tutorial relies on them).
        if (penetrateObjs == null)
            penetrateObjs = new List<RectTransform>();
        mask = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        foreach (var penetrateObj in penetrateObjs)
        {
            if (penetrateObj == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(penetrateObj, eventData.position, eventData.pressEventCamera))
            {
                ExecuteEvents.Execute(penetrateObj.gameObject, eventData, ExecuteEvents.pointerClickHandler);
                AdvanceGuide();
                return;
            }
        }

        // Other areas keep original behavior
        if (mask != null && mask.raycastTarget)
        {
            // Mask click absorbed; no advance.
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        foreach (var penetrateObj in penetrateObjs)
        {
            if (penetrateObj == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(penetrateObj, eventData.position, eventData.pressEventCamera))
            {
                ExecuteEvents.Execute(penetrateObj.gameObject, eventData, ExecuteEvents.pointerDownHandler);
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        foreach (var penetrateObj in penetrateObjs)
        {
            if (penetrateObj == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(penetrateObj, eventData.position, eventData.pressEventCamera))
            {
                ExecuteEvents.Execute(penetrateObj.gameObject, eventData, ExecuteEvents.pointerUpHandler);
            }
        }
    }

    private static void AdvanceGuide()
    {
        // WZ Goal/Withdraw tutorial owns the click when active.
        var wzTutorial = WZSDK.Tutorial.instance;
        if (wzTutorial != null && wzTutorial.isActiveAndEnabled)
        {
            wzTutorial.ExecuteCurrentStepCallback();
            return;
        }

        FacadeGuide.NextStep?.Invoke();
    }
}
