using System.Collections.Generic;
using System.Linq;

public class HighlightManager
{
    private PlayerController playerController;

    private Dictionary<Entity, List<Unit>> highlightedEnemies = new Dictionary<Entity, List<Unit>>();

    public void HighlightEnemy(Unit unit, Entity target, bool choose)
    {
        if (target == null || unit.PlayerID != playerController.GetID() || target.PlayerID == playerController.GetID()) return;

        if (highlightedEnemies.ContainsKey(target))
        {
            var list = highlightedEnemies[target];

            if (list.Contains(unit))
            {
                if (!choose)
                    RemoveTargeting(list, unit, target);
            }
            else if (choose)
            {
                list.Add(unit);
                Targeting(target);
            }
        }
        else if (choose)
        {
            highlightedEnemies.Add(target, new List<Unit> { unit });
            Targeting(target);
        }
    }

    private void RemoveTargetFromHighlightedEnemy(Entity target)
    {
        if (target != null && highlightedEnemies.ContainsKey(target))
            Untargeting(target);
    }

    public void RemoveUnitFromHighlightedEnemy(Unit unit, Entity target)
    {
        if (target != null && highlightedEnemies.ContainsKey(target) && highlightedEnemies[target].Contains(unit))
        {
            var list = highlightedEnemies[target];

            RemoveTargeting(list, unit, target);
        }
    }

    private void RemoveTargeting(List<Unit> list, Unit unit, Entity target)
    {
        list.Remove(unit);
        
        if (list.Count < 1)
            Untargeting(target);
    }

    private void Targeting(Entity target)
    {
        target.Targeting(true);
        target.OnDeath += RemoveTargetFromHighlightedEnemy;
    }

    private void Untargeting(Entity target)
    {
        highlightedEnemies.Remove(target);
        target.Targeting(false);
        target.OnDeath -= RemoveTargetFromHighlightedEnemy;
    }

    public void Choose(Entity entity, bool turnOn) => entity.Choose(turnOn);

    public void RemoveAllTargeting()
    {
        foreach(var targets in highlightedEnemies)
            targets.Key.Targeting(false);

        highlightedEnemies.Clear();
    }

    public void RemoveChooseInList(List<Entity> entities)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] != null)
                entities[i].Choose(false);
        }
    }

    public HighlightManager(PlayerController playerController)
    {
        this.playerController = playerController;
    }
}
