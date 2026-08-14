using UnityEngine;
using UnityEngine.UI;

public class EntityTeamView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] teamSprites;
    [SerializeField] private SpriteRenderer teamMapIcon;

    void Start()
    {
        Transform body = transform.Find("Body");
        
        teamSprites = new SpriteRenderer[body.childCount];

        for (int i = 0; i < body.childCount; i++)
        {
            if (body.GetChild(i).TryGetComponent(out SpriteRenderer sprite))
                teamSprites[i] = sprite;
        }

        if (teamMapIcon == null && transform.Find("Map_Icon") != null)
            teamMapIcon = transform.Find("Map_Icon").GetComponent<SpriteRenderer>();
    }

    public void SetColor(int ownerPlayerID, int localPlayerID)
    {
        if (ownerPlayerID == localPlayerID) return;

        if (teamSprites != null)
        {
            foreach(SpriteRenderer sprite in teamSprites)
                sprite.color = Color.red;
        }

        if (teamMapIcon != null)
            teamMapIcon.color = Color.red;
    }
}
