using UnityEngine;

public class ChooseBox : MonoBehaviour
{
    private PlayerController playerController;

    private bool isActive;

    void Awake()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void OnEnable()
    {
        int unitLayer = 1 << 6; // 6 - Units
        int buildingLayer = 1 << 7; // 7 - Buildings
        int constructionLayer = 1 << 8; // 7 - Buildings

        int combinedLayerMask = unitLayer | buildingLayer | constructionLayer;

        Collider2D[] hits;

        hits = Physics2D.OverlapPointAll(transform.position, combinedLayerMask);

        if (hits != null)
        {
            foreach(Collider2D hit in hits)
                Choose(hit, true);
        }
    }

    private void Choose(Collider2D collision, bool choose) => playerController.SetChoose(collision, choose);

    public void SetActive(bool active)
    {
        isActive = active;

        gameObject.SetActive(active);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActive)
            Choose(collision, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isActive)
            Choose(collision, false);
    }
}
