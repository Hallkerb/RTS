using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    void Update()
    {
        transform.position = new Vector2((Input.mousePosition.x - Screen.width / 2) / 4000, (Input.mousePosition.y - Screen.height / 2) / 4000);
    }
}