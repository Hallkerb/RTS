using UnityEngine;

public struct EntityShape
{
    public enum ShapeType { Square, Circle }

    public ShapeType Type { get; private set; }
    public float SizeX { get; private set; }
    public float SizeY { get; private set; }

    public bool IsInside(Vector2 startPoint, Vector2 targetPoint, float stepSize = 0)
    {
        bool isInside = false;

        switch(Type)
        {
            case ShapeType.Square:
                float dx = Mathf.Abs(startPoint.x - targetPoint.x);
                float dy = Mathf.Abs(startPoint.y - targetPoint.y);
                isInside = dx <= stepSize + (SizeX / 2) && dy <= stepSize + (SizeY / 2);
                break;
            case ShapeType.Circle:
                float distance = Vector2.Distance(startPoint, targetPoint);
                isInside = distance <= stepSize + (SizeX / 2);
                break;
        }

        return isInside;
    }

    public EntityShape(ShapeType shape, float sizeX, float sizeY)
    {
        Type = shape;
        SizeX = sizeX;
        SizeY = sizeY;
    }
}
