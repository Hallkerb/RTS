using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyScanner
{
    public bool EnemySearch { get; set; }

    private int combinedLayerMask;

    public Coroutine SearchCoroutine;

    public IEnumerator SearchEnemy(Vector2 position, float radius, int playerID, Action<Entity> task, IReadOnlyCollection<(Action task, Entity target)> tasksQueue)
    {
        EnemySearch = true;

        Collider2D[] hits;

        while(EnemySearch)
        {
            if (tasksQueue.Count > 0) break;

            hits = Physics2D.OverlapCircleAll(position, radius, combinedLayerMask);
            Entity enemy = hits.Select(h => h.GetComponent<Entity>())
                            .Where(e => e != null)
                            .Where(e => e.PlayerID != playerID)
                            .FirstOrDefault();

            if (enemy != null)
            {
                task(enemy);
                break;
            }

            yield return new WaitForSeconds(1);
        }

        EnemySearch = false;
    }

    public EnemyScanner()
    {
        int unitLayer = 1 << 6; // 6 - Units
        int buildingLayer = 1 << 7; // 7 - Buildings

        combinedLayerMask = unitLayer | buildingLayer;
    }
}
