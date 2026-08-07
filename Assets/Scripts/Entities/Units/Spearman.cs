using Mirror;
using UnityEngine;

public class Spearman : Unit
{
    private Transform spear;
    [SerializeField] private Vector2 startPosSpear;
    [SerializeField] private Vector2 endPosSpear;

    protected override void Awake()
    {
        base.Awake();

        spear = Body.Find("Spear");
    }

    [ClientRpc]
    protected override void RpcAttackAnimate()
    {
        spear.localPosition = endPosSpear;
    }

    [ClientRpc]
    protected override void RpcReloadAnimate(float t)
    {
        spear.localPosition = Vector2.Lerp(endPosSpear, startPosSpear, t);
    }
}
