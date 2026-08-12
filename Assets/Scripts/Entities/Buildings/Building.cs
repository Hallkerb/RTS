using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Building : Entity
{
    protected PlayerController playerController;

    protected override void Awake()
    {
        base.Awake();

        playerController = FindFirstObjectByType<PlayerController>();
    }

    [Client]
    public override void Choose(bool choose)
    {
        base.Choose(choose);

        OpenUI(choose);
    }

    protected abstract void OpenUI(bool choose);
}