using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    protected PlayerController playerController;

    [SerializeField] protected string NameText;
    [SerializeField] protected string DescriptionText;

    protected virtual void Awake()
    {
        playerController = Camera.main.GetComponent<PlayerController>();
    }

    public virtual void SetInfo(string nameText, string descriptionText)
    {
        NameText = nameText;
        DescriptionText = descriptionText;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (InfoPanel.Singlton == null) return;

        InfoPanel.Singlton.SetInfo(gameObject, NameText, DescriptionText);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        RemoveInfoPanel();
    }

    private void RemoveInfoPanel()
    {
        if (InfoPanel.Singlton == null) return;

        InfoPanel.Singlton.Remove();
    }
}