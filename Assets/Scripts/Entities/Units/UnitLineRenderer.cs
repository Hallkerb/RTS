using Mirror;
using UnityEngine;

public class UnitLineRenderer : NetworkBehaviour
{
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
    }

    [TargetRpc]
    public void ResetLineRendererPosition(NetworkConnection target) => lineRenderer.positionCount = 0;

    [TargetRpc]
    public void ReductionLineRendererPosition(NetworkConnection target)
    {
        if(lineRenderer.positionCount > 0)
            lineRenderer.positionCount--;
    }

    [TargetRpc]
    public void SetLineRendererPosition(NetworkConnection target)
    {
        if(lineRenderer.positionCount > 0)
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);
    }

    [TargetRpc]
    public void ReconstructLineRenderer(NetworkConnection target, Vector2[] wayPoints)
    {
        lineRenderer.positionCount = wayPoints.Length + 1;

        lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);

        for (int i = lineRenderer.positionCount - 2; i >= 0; i--)
        {
            lineRenderer.SetPosition(i, wayPoints[wayPoints.Length - 1 - i]);
        }
    }
}