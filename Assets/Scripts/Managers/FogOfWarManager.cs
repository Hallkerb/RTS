using UnityEngine;
using UnityEngine.Rendering;

public class FogOfWarManager : MonoBehaviour
{
    public static FogOfWarManager Instance;

    [SerializeField] private Material fogMemoryMat;

    [SerializeField] private RenderTexture visibilityRT;
    [SerializeField] private RenderTexture entitiesRT;
    [SerializeField] private RenderTexture exploredRT_A;
    [SerializeField] private RenderTexture exploredRT_B;

    [SerializeField] private Vector2 worldSize;

    private bool ping;

    public RenderTexture CurrentExploredRT => ping ? exploredRT_B : exploredRT_A;

    void Awake()
    {
        Instance = this;

        ClearRT(visibilityRT);
        ClearRT(entitiesRT);
        ClearRT(exploredRT_A);
        ClearRT(exploredRT_B);
    }

    void Start()
    {
        worldSize = new Vector2(Floor.Instance.SizeX.x, Floor.Instance.SizeY.x);
    }

    void LateUpdate()
    {
        RenderTexture src = ping ? exploredRT_A : exploredRT_B;
        RenderTexture dst = ping ? exploredRT_B : exploredRT_A;

        fogMemoryMat.SetTexture("_VisibilityTex", visibilityRT);
        fogMemoryMat.SetTexture("_PrevExploredTex", src);
        fogMemoryMat.SetTexture("_EntitiesTex", entitiesRT);

        Graphics.Blit(null, dst, fogMemoryMat);

        ping = !ping;
    }

    void ClearRT(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = prev;
    }

    public bool IsPositionVisible(Vector2 worldPos)
    {
        return CheckPixelState(visibilityRT, worldPos);
    }

    public bool IsPositionExplored(Vector2 worldPos)
    {
        return CheckPixelState(CurrentExploredRT, worldPos);
    }

    private bool CheckPixelState(RenderTexture rt, Vector2 worldPos)
    {
        if (rt == null) return false;

        Vector2 worldOrigin = worldSize / 2 - worldSize;

        float uvX = (worldPos.x - worldOrigin.x) / worldSize.x;
        float uvY = (worldPos.y - worldOrigin.y) / worldSize.y;

        if (uvX < 0f || uvX > 1f || uvY < 0f || uvY > 1f) return false;

        int pixelX = Mathf.Clamp((int)(uvX * rt.width), 0, rt.width - 1);
        int pixelY = Mathf.Clamp((int)(uvY * rt.height), 0, rt.height - 1);

        var request = AsyncGPUReadback.Request(rt, 0, pixelX, 1, pixelY, 1, 0, 1);
        request.WaitForCompletion();

        if (request.hasError) return false;

        var data = request.GetData<Color32>();
        if (data.Length > 0)
            return data[0].a > 128 || data[0].r > 128; 

        return false;
    }
}