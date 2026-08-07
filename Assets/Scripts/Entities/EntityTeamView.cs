using UnityEngine;
using UnityEngine.UI;

public class EntityTeamView : MonoBehaviour
{
    [SerializeField] private Image[] teamImages;
    [SerializeField] private SpriteRenderer teamMapIcon;

    public void SetColor(int ownerPlayerID, int localPlayerID)
    {
        if (ownerPlayerID == localPlayerID) return;

        if (teamImages != null)
        {
            foreach(Image image in teamImages)
                image.color = Color.red;
        }

        if (teamMapIcon == null && transform.Find("Map_Icon") != null)
        {
            teamMapIcon = transform.Find("Map_Icon").GetComponent<SpriteRenderer>();
            teamMapIcon.color = Color.red;
        }
    }
}
