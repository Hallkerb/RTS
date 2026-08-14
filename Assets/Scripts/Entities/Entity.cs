using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class EntityData
{
    [SerializeField] private string name;
    public string Name => name;
}

public abstract class Entity : NetworkBehaviour, IPoolable
{
    protected Entity Prefub;
    [SerializeField] private EntityData data;
    private EntityShape shape;

    protected Rigidbody2D rb;

    private GameObject isChooseObj;
    private GameObject isTargetObj;
    private GameObject hpObj;

    public Transform ViewingCircle { get; private set; }

    private Image hpImage;

    public event Action<Entity> OnDeath;

    [SyncVar]
    [SerializeField] private int playerID = -1;
    public int PlayerID
    {
        get => playerID;
        private set => playerID = value;
    }

    [SyncVar(hook = nameof(UpdateHPImage))]
    [SerializeField] private float hp;
    public float HP
    {
        get => hp;
        protected set
        {
            hp = value;

            if (hp <= 0)
                Destruction();
        }
    }

    public float ViewingRadius { get; private set; }

    [SyncVar]
    protected float startHP;

    public EntityData Data => data;
    public EntityShape Shape => shape;

    protected virtual void Awake()
    {
        Collider2D сollider = GetComponent<Collider2D>();

        EntityShape.ShapeType shapeType;

        switch (сollider)
        {
            case BoxCollider2D _:
                shapeType = EntityShape.ShapeType.Square;
                break;
            case CircleCollider2D _:
                shapeType = EntityShape.ShapeType.Circle;
                break;
            default:
                shapeType = EntityShape.ShapeType.Square;
                break;
        }

        float SizeX = сollider.bounds.max.x - сollider.bounds.min.x;
        float SizeY = сollider.bounds.max.y - сollider.bounds.min.y;

        shape = new EntityShape(shapeType, SizeX, SizeY);

        if (TryGetComponent(out Rigidbody2D rigidbody))
            rb = rigidbody;

        Transform ui = transform.Find("UI");

        isChooseObj = ui.Find("Choose").gameObject;
        isTargetObj = ui.Find("Target").gameObject;
        hpObj = ui.Find("HP_Background").gameObject;

        hpImage = hpObj.transform.Find("HP_Image").GetComponent<Image>();

        startHP = hp;

        ViewingCircle = transform.Find("ViewingCircle");

        if (ViewingCircle != null)
        {
            ViewingRadius = ViewingCircle.localScale.x / 2;
            
            StartCoroutine(AnimateViewingCircle());
        }
    }

    protected virtual void Start()
    {
        Prefub = GetComponent<PoolableObject>().OriginPrefab.GetComponent<Entity>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        StartCoroutine(InitializeTeamProperties());
    }

    [Client]
    public void InitializeTeamColor(int localPlayerID)
    {
        Debug.Log($"LocalPlayerID: {localPlayerID}, PlayerID: {PlayerID}");

        if (PlayerID == localPlayerID) return;

        if (TryGetComponent(out EntityTeamView view))
            view.SetColor(PlayerID, localPlayerID);
        else
            Debug.LogError("EntityTeamView is missing. Team color for entity didn`t change!");
    }

    private IEnumerator InitializeTeamProperties()
    {
        while (NetworkClient.localPlayer == null || NetworkClient.localPlayer.GetComponent<Player>().IsInitialized == false || PlayerID < 0) yield return null;

        int localID = NetworkClient.localPlayer.GetComponent<Player>().ID;

        if (PlayerID == localID) 
        {
            if (transform.Find("ViewingCircle") != null)
                transform.Find("ViewingCircle").gameObject.layer = 9; // Fog visible layer

            yield break;
        }

        if (TryGetComponent(out EntityTeamView view))
            view.SetColor(PlayerID, localID);
        else
            Debug.LogError("EntityTeamView is missing. Team color for entity didn`t change!");
    }

    private IEnumerator AnimateViewingCircle()
    {
        Vector3 start = new Vector3(0, 0, ViewingCircle.localScale.z);
        Vector3 end = new Vector3(ViewingRadius * 2, ViewingRadius * 2, ViewingCircle.localScale.z);
        
        float duration = 0.5f;
        float timeLeft = 0;

        while(timeLeft < duration)
        {
            timeLeft += Time.deltaTime;

            ViewingCircle.localScale = Vector3.Lerp(start, end, timeLeft / duration);

            yield return null;
        }

        ViewingCircle.localScale = end;
    }

    public bool IsChosen() => isChooseObj.activeSelf;

    private void UpdateHPImage(float oldValue, float newValue)
    {
        if (newValue < startHP)
        {
            hpObj.SetActive(true);
            
            hpImage.fillAmount = HP / startHP;
        }
        else
            hpObj.SetActive(false);
    }

    [Client]
    public virtual void Choose(bool choose) => isChooseObj.SetActive(choose);

    [Client]
    public void Targeting(bool turnOn) => isTargetObj.SetActive(turnOn);

    [Server]
    public virtual void InflictDamage(float enemyDamage, Entity enemy)
    {
        HP -= enemyDamage;
    }

    [Server]
    public virtual void Initialize(int playerID)
    {
        PlayerID = playerID;
    }

    public virtual void ResetState()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
        }

        if (TryGetComponent(out NetworkTransformBase networkTransform))
        {
            networkTransform.Reset();
        }

        if (Prefub != null)
        {
            gameObject.layer = Prefub.gameObject.layer;
            hp = Prefub.HP;
            startHP = hp;
        }
    }

    public virtual async void LocalDestruction()
    {
        OnDeath?.Invoke(this);

        await Task.Yield();
        ObjectPooler.Instance.SetToPool(gameObject);
    }

    [Server]
    public virtual async void Destruction()
    {
        OnDeath?.Invoke(this);
        
        RpcNotifyDeath();

        await Task.Yield();

        UnSpawn();
    }

    [ClientRpc]
    private void RpcNotifyDeath()
    {
        if (isServer) return;

        OnDeath?.Invoke(this);
    }

    protected void UnSpawn() => NetworkServer.UnSpawn(gameObject);
}
