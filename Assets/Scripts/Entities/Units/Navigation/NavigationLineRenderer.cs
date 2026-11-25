using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class NavigationLineRenderer : MonoBehaviour
{
    public static NavigationLineRenderer Instance;

    private Player player;
    private PlayerController playerController;
    private LineRenderer lineRenderer;

    private Unit chosenUnit;

    void Awake()
    {
        Instance = this;

        lineRenderer = gameObject.GetComponent<LineRenderer>();
    }

    void Start()
    {
        playerController = PlayerController.Instance;
        playerController.OnUnitChooseChanged += HandleUnitChooseChanged;

        Disable();
    }

    void Update()
    {
        if (chosenUnit == null) return;

        lineRenderer.SetPosition(lineRenderer.positionCount - 1, chosenUnit.transform.position);
    }

    private void HandleUnitChooseChanged()
    {
        if (playerController.UnitChoose.Count == 1)
            Enable(playerController.UnitChoose[0] as Unit);
        else
            Disable();
    }

    public void Enable(Unit unit)
    {
        RequestNavigation(unit);

        gameObject.SetActive(true);
    }

    public void Disable()
    {
        if (chosenUnit != null)
            chosenUnit.CmdRequestUnhandleNavigation(player.ID);

        gameObject.SetActive(false);
    }

    private void RequestNavigation(Unit unit)
    {
        chosenUnit = unit;

        if (chosenUnit == null) return;

        chosenUnit.CmdRequestUnitNavigation(player.ID);
    }

    public void SetNavigation(Vector2[] wayPoints)
    {
        lineRenderer.positionCount = wayPoints.Length + 1;

        lineRenderer.SetPosition(lineRenderer.positionCount - 1, chosenUnit.transform.position);

        for (int i = lineRenderer.positionCount - 2; i >= 0; i--)
        {
            lineRenderer.SetPosition(i, wayPoints[wayPoints.Length - 1 - i]);
        }
    }

    public void Initialize(Player player)
    {
        this.player = player;
    }
}
