using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    protected PlayerController playerController;

    [SerializeField] protected string NameText;
    [SerializeField] protected string DescriptionText;

    protected virtual void Awake()
    {
        playerController = Camera.main.GetComponent<PlayerController>();
    }

    protected virtual void SetInfo(string nameText, string descriptionText)
    {
        NameText = nameText;
        DescriptionText = descriptionText;
    }

    protected virtual bool TrySetInfo(PointerEventData eventData)
    {
        if (InfoPanel.Singlton == null) return false;

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;

        if (hitObject == null || InfoPanel.Singlton.Owner == hitObject) return false;

        return true;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (TrySetInfo(eventData) == false) return;

        InfoPanel.Singlton.SetInfo(eventData.pointerCurrentRaycast.gameObject, NameText, DescriptionText);
    }

    public virtual void OnPointerMove(PointerEventData eventData)
    {
        if (TrySetInfo(eventData) == false) return;

        InfoPanel.Singlton.SetInfo(eventData.pointerCurrentRaycast.gameObject, NameText, DescriptionText);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (InfoPanel.Singlton == null) return;

        InfoPanel.Singlton.Remove(eventData.pointerCurrentRaycast.gameObject);
    }
}