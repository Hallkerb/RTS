using UnityEngine;

public class Fog : MonoBehaviour
{
    [SerializeField] private Material fogMemoryMat;

    [SerializeField] private RenderTexture visibilityRT;
    [SerializeField] private RenderTexture entitiesRT;
    [SerializeField] private RenderTexture exploredRT_A;
    [SerializeField] private RenderTexture exploredRT_B;

    private bool ping;

    void Awake()
    {
        ClearRT(visibilityRT);
        ClearRT(entitiesRT);
        ClearRT(exploredRT_A);
        ClearRT(exploredRT_B);
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
}