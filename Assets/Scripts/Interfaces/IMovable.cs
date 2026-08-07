using UnityEngine;

public interface IMovable
{
    void SetMoveTask(NavigationMode mode, Entity target, Vector2 position, int? fieldIndex, bool resetTasks = true);
}
