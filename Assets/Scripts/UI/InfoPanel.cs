using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    public static InfoPanel Singlton;
    private GameObject currentOwner;

    private TextMeshProUGUI nameText;
    private TextMeshProUGUI descriptionText;

    public GameObject Owner => currentOwner;

    void Awake()
    {
        Singlton = this;

        Transform texts = transform.Find("Texts");
        nameText = texts.Find("Name").GetComponent<TextMeshProUGUI>();
        descriptionText = texts.Find("Description").GetComponent<TextMeshProUGUI>();

        gameObject.SetActive(false);
    }

    void Update()
    {
        Move();

        if (currentOwner.activeInHierarchy == false)
            Remove(currentOwner);
    }

    private void Move()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = mousePos + Vector2.up * 1.5f;
    }

    public async void SetInfo(GameObject owner, string name, string description)
    {
        currentOwner = owner;

        nameText.text = name;
        descriptionText.text = description;

        Move();

        gameObject.SetActive(true);
    }

    public void Remove(GameObject owner)
    {
        if (owner != null && owner == currentOwner) return;

        currentOwner = null;

        gameObject.SetActive(false);
    }
}
