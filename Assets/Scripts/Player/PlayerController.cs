using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private Player player;
    private PlayerUnitsController playerUnitsController;
    private HighlightManager highlightManager;
    [HideInInspector] public UI UserInterface;

    [SerializeField] private ChooseBox chooseBox;

    [SerializeField] private float zoomSensitivity;
    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;

    private Vector3 dragOriginWorld;

    private Vector2 startChoosePos;

    public List<Entity> UnitChoose { get; private set; } = new List<Entity>();
    public List<Entity> BuildingChoose { get; private set; } = new List<Entity>();
    public List<Entity> ConstructionChoose { get; private set; } = new List<Entity>();

    public Dictionary<string, List<SpawnerBuild>> SpawnerBuildingsByType { get; private set; } = new Dictionary<string, List<SpawnerBuild>>();

    private BuildingPhantom constructionPhantom;
    private int constrIndex = -1;

    public Player Player => player;

    public int PlayerID => player.ID;

    void Awake()
    {
        highlightManager = new HighlightManager(this);
        UserInterface = FindFirstObjectByType<UI>();

        MiniMap.OnDragMap += MoveCameraOnMap;
        MiniMap.OnSetTask += SetTaskOnMap;

        for (int i = 0; i < UI.PanelKeys.Length; i++)
        {
            SpawnerBuildingsByType.Add(UI.PanelKeys[i], new List<SpawnerBuild>());
        }
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject() == false && Input.GetMouseButtonDown(0))
        {
            if (constructionPhantom != null)
                Construct();
            else
                StartChoose();
        }

        if (EventSystem.current.IsPointerOverGameObject() == false && Input.GetMouseButton(0))
            UpdateChooseBox();

        if (Input.GetMouseButtonUp(0))
            EndChoose();

        if (Input.GetMouseButtonDown(1))
            SetTask(Input.mousePosition);

        if (Input.GetMouseButtonDown(2))
            dragOriginWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(2)) // 2 - middle button
            MoveCamera();

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            Camera.main.orthographicSize -= scroll * zoomSensitivity;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minZoom, maxZoom);
        }

        if (Input.GetKeyUp(KeyCode.Delete))
            player.DeleteEntity();

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (UserInterface.exitGamePanel.activeSelf || (UnitChoose.Count <= 0 && BuildingChoose.Count <= 0 && ConstructionChoose.Count <= 0 && constructionPhantom == null))
                UserInterface.exitGamePanel.SetActive(!UserInterface.exitGamePanel.activeSelf);
            
            ClearChoose();
            SetСonstructionBuilding();
        }
    }

    private void SetTask(Vector2 mousePosition)
    {
        if (player.IsLoss) return;

        Vector2 targetPoistion = Camera.main.ScreenToWorldPoint(mousePosition);

        SetСonstructionBuilding();
        playerUnitsController.CmdSetUnitsTasks(UnitChoose.OfType<Unit>().ToArray(), targetPoistion, player.ID);
        SetBuildingsSpawnPos();
    }

    private void SetTaskOnMap(Vector2 mousePosition)
    {
        if (player.IsLoss) return;

        Vector2 floorHalfSize = Floor.Instance.transform.localScale / 2;
        Vector3 targetPoistion = Floor.Instance.IsCameraInside(new Vector3(mousePosition.x * floorHalfSize.x, mousePosition.y * floorHalfSize.y, Camera.main.transform.position.z)).pos;

        SetСonstructionBuilding();
        playerUnitsController.CmdSetUnitsTasks(UnitChoose.OfType<Unit>().ToArray(), targetPoistion, player.ID);
        SetBuildingsSpawnPos();
    }

    private void ClearChoose()
    {
        highlightManager.RemoveAllTargeting();
        highlightManager.RemoveChooseInList(UnitChoose);
        highlightManager.RemoveChooseInList(BuildingChoose);
        highlightManager.RemoveChooseInList(ConstructionChoose);
        UnitChoose.Clear();
        BuildingChoose.Clear();
        ConstructionChoose.Clear();
        SpawnerBuildingsByType.Clear();

        foreach (var key in UI.PanelKeys)
            UserInterface.QueueUI.ClosePanel(key);
    }

    private void StartChoose()
    {
        ClearChoose();

        chooseBox.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        chooseBox.SetActive(true);

        startChoosePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private void EndChoose()
    {
        chooseBox.SetActive(false);

        for (int i = 0; i < UI.PanelKeys.Length; i++)
        {
            if (SpawnerBuildingsByType[UI.PanelKeys[i]].Count > 0)
            {
                List<QueueTaskData> queueTaskDatas = new List<QueueTaskData>();
                
                for (int j = 0; j < SpawnerBuildingsByType[UI.PanelKeys[i]].Count; j++)
                {
                    queueTaskDatas.AddRange(SpawnerBuildingsByType[UI.PanelKeys[i]][j].GetQueueData());
                }

                UserInterface.QueueUI.OpenPanel(UI.PanelKeys[i], queueTaskDatas);
            }
        }
    }

    public void SetChoose(Collider2D collision, bool choose)
    {
        if (!collision.TryGetComponent(out Entity chosen) || (chosen.PlayerID != PlayerID && FogOfWarManager.Instance.IsPositionVisible(chosen.transform.position))) return;

        switch(chosen)
        {
            case Unit unit:
                if (choose && !UnitChoose.Contains(unit))
                {
                    UnitChoose.Add(unit);

                    unit.ChangeLocalTarget -= ReactOnChangeLocalTarget;
                    unit.ChangeLocalTarget += ReactOnChangeLocalTarget;
                    unit.OnDeath += ReactOnDeathEntity;
                }
                else if (!choose)
                {
                    UnitChoose.Remove(unit);

                    unit.ChangeLocalTarget -= ReactOnChangeLocalTarget;
                    unit.OnDeath -= ReactOnDeathEntity;
                }

                highlightManager.HighlightEnemy(unit, unit.Target, choose);
                break;
            
            case Building building:
                if (choose && !BuildingChoose.Contains(building))
                {
                    BuildingChoose.Add(building);
                    building.OnDeath += ReactOnDeathEntity;

                    if (building is ISpawner spawner && SpawnerBuildingsByType[UI.PanelKeys[spawner.IndexUI]].Contains(building) == false)
                    {
                        SpawnerBuildingsByType[UI.PanelKeys[spawner.IndexUI]].Add(building as SpawnerBuild);
                    }
                }
                else if (!choose)
                {
                    BuildingChoose.Remove(building);
                    building.OnDeath -= ReactOnDeathEntity;

                    if (building is ISpawner spawner)
                    {
                        SpawnerBuildingsByType[UI.PanelKeys[spawner.IndexUI]].Remove(building as SpawnerBuild);
                    }
                }
                break;
            case BuildingPhantom phantom:
                if (choose && !ConstructionChoose.Contains(phantom))
                {
                    ConstructionChoose.Add(phantom);
                    phantom.OnDeath += ReactOnDeathEntity;
                }
                else if (!choose)
                {
                    ConstructionChoose.Remove(phantom);
                    phantom.OnDeath -= ReactOnDeathEntity;
                }
                break;
        }

        highlightManager.Choose(chosen, choose);
    }

    private void Construct()
    {
        if (constructionPhantom.PosClear == false) return;
        
        bool resetTasks = true;

        if (Input.GetKey(KeyCode.LeftShift))
            resetTasks = false;

        constructionPhantom.Place(player, resetTasks, constrIndex);
        constructionPhantom = null;

        if (Input.GetKey(KeyCode.LeftShift))
            UserInterface.BuildingsUI.SpawnBuilding(constrIndex);
        else
            constrIndex = -1;
    }

    private void ReactOnChangeLocalTarget(Unit unit, (Entity oldTarget, Entity newTarget) targets)
    {
        highlightManager.HighlightEnemy(unit, targets.newTarget, unit.IsChosen());
        highlightManager.RemoveUnitFromHighlightedEnemy(unit, targets.oldTarget);
    }

    private void ReactOnDeathEntity(Entity entity)
    {
        entity.OnDeath -= ReactOnDeathEntity;

        switch(entity)
        {
            case Unit unit:
                UnitChoose.Remove(unit);
                break;
            case Building building:
                BuildingChoose.Remove(building);
                break;
            case BuildingPhantom phantom:
                ConstructionChoose.Remove(phantom);
                break;
        }
    }

    public void SetСonstructionBuilding(BuildingPhantom phantom = null, int index = -1)
    {
        if (constructionPhantom != null)
            Destroy(constructionPhantom.gameObject);

        constructionPhantom = phantom;
        constrIndex = index;
    }

    private void SetBuildingsSpawnPos()
    {
        if (BuildingChoose.Count > 0)
        {
            Vector2 mousePosition = Input.mousePosition;

            Vector2 targetPoistion = Camera.main.ScreenToWorldPoint(mousePosition);

            foreach (Building building in BuildingChoose)
            {
                if (building.TryGetComponent(out SpawnerBuild SpawnBuild))
                {
                    SpawnBuild.CmdRepositionSpawn(targetPoistion);
                }
            }
        }
    }

    private void UpdateChooseBox()
    {
        Vector2 difference = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - startChoosePos;

        chooseBox.transform.position = startChoosePos + difference / 2;

        float sizeX = Camera.main.ScreenToWorldPoint(Input.mousePosition).x - startChoosePos.x;
        float sizeY = Camera.main.ScreenToWorldPoint(Input.mousePosition).y - startChoosePos.y;

        chooseBox.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Abs(sizeX), Mathf.Abs(sizeY));
        chooseBox.GetComponent<BoxCollider2D>().size = chooseBox.GetComponent<RectTransform>().sizeDelta;
    }

    private void MoveCamera()
    {
        Vector3 currentMouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 difference = dragOriginWorld - currentMouseWorld;

        var cameraMovement = Floor.Instance.IsCameraInside(Camera.main.transform.position + difference);

        if (cameraMovement.inside == false)
            dragOriginWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Camera.main.transform.position = cameraMovement.pos;
    }

    private void MoveCameraOnMap(Vector2 mousePosition)
    {
        Vector2 floorHalfSize = Floor.Instance.transform.localScale / 2;
        Vector3 pos = Floor.Instance.IsCameraInside(new Vector3(mousePosition.x * floorHalfSize.x, mousePosition.y * floorHalfSize.y, Camera.main.transform.position.z)).pos;

        Camera.main.transform.position = pos;
    }

    public void SetCameraPosition(Vector2 pos) => Camera.main.transform.position = new Vector3(pos.x, pos.y, Camera.main.transform.position.z);

    private void Win()
    {
        SetСonstructionBuilding();

        UserInterface.OpenWinUI();
    }

    private void Lose()
    {
        SetСonstructionBuilding();

        UserInterface.OpenLoseUI();
    }

    public void Initialize(Player player, PlayerUnitsController playerUnitsController)
    {
        this.player = player;
        this.playerUnitsController = playerUnitsController;

        if (UserInterface.exitGamePanel != null)
        {
            var exitButton = UserInterface.exitGamePanel.transform.Find("Exit_Button").GetComponent<Button>();

            exitButton.onClick.AddListener(playerUnitsController.DisposeNavigationManager);
            exitButton.onClick.AddListener(player.StopClient);
        }

        this.player.OnWin += Win;
        this.player.OnLose += Lose;

        FindFirstObjectByType<ResourcesUI>().Initialize(player.Economy);
    }
}
