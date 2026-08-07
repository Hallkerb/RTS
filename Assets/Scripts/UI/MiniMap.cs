using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniMap : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IDragHandler
{
    private RectTransform rectTransform;

    public static event Action<Vector2> OnDragMap;
    public static event Action<Vector2> OnSetTask;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            SendNormalizedPosition(eventData, OnDragMap);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            SendNormalizedPosition(eventData, OnSetTask);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            SendNormalizedPosition(eventData, OnDragMap);
    }

    private void SendNormalizedPosition(PointerEventData eventData, Action<Vector2> action)
    {
        Vector2 localPoint;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Rect r = rectTransform.rect;

            float x01 = (localPoint.x - r.x) / r.width;
            float y01 = (localPoint.y - r.y) / r.height;

            float xFinal = x01 * 2f - 1f;
            float yFinal = y01 * 2f - 1f;

            Vector2 finalPos = new Vector2(Mathf.Clamp(xFinal, -1f, 1f), Mathf.Clamp(yFinal, -1f, 1f));

            action?.Invoke(finalPos);
        }
    }
}