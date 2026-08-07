using UnityEngine;

public class CameraScaleFit : MonoBehaviour
{
    private Camera cam;
    private Transform[] borders = new Transform[4];

    [SerializeField] private float Size = 10;

    void Start()
    {
        cam = GetComponentInParent<Camera>();

        RectTransform rectTransform = transform as RectTransform;

        for(int i = 0; i < borders.Length; i++)
            borders[i] = transform.GetChild(i);

        float Height = rectTransform.sizeDelta.y;
        float Width = rectTransform.sizeDelta.x;
        
        borders[0].localScale = new Vector2(Size, Height);
        borders[1].localScale = new Vector2(Size, Height);
        borders[2].localScale = new Vector2(Width, Size);
        borders[3].localScale = new Vector2(Width, Size);
    }

    void Update()
    {
        ChangeSize();
    }

    private void ChangeSize()
    {
        float newSize = Size * (Size / cam.orthographicSize);

        borders[0].localScale = new Vector2(newSize, borders[0].localScale.y);
        borders[1].localScale = new Vector2(newSize, borders[1].localScale.y);
        borders[2].localScale = new Vector2(borders[2].localScale.x, Size);
        borders[3].localScale = new Vector2(borders[3].localScale.x, Size);
    }
}
