using UnityEngine;

public static class GeometryUtils
{
    public static bool IsInside(Vector2 nodePosition, BuildingData buildingData)
    {
        float padding = 0.01f;

        Vector2 localPos = Quaternion.Inverse(buildingData.rotation) * (nodePosition - buildingData.position);
        Vector2 halfSize = buildingData.size * 0.5f;

        return Mathf.Abs(localPos.x) <= halfSize.x + 0.5f + padding &&
            Mathf.Abs(localPos.y) <= halfSize.y + 0.5f + padding;
    }

    public static bool IsInside(Vector2 firstPosition, float firstSize, Vector2 secondPosition, Vector2 secondSize)
    {
        float halfFirstSize = firstSize / 2;
        Vector2 halfSecondSize = new Vector2(secondSize.x / 2, secondSize.y / 2);

        return firstPosition.x - halfFirstSize < secondPosition.x + halfSecondSize.x && firstPosition.x + halfFirstSize > secondPosition.x - halfSecondSize.x &&
           firstPosition.y - halfFirstSize < secondPosition.y + halfSecondSize.y && firstPosition.y + halfFirstSize > secondPosition.y - halfSecondSize.y;
    }

    public static bool IsInsideSquare(Vector2 firstPosition, Vector2 firstSize, Vector2 secondPosition, Vector2 secondSize)
    {
        Vector2 halfFirst = firstSize * 0.5f;
        Vector2 halfSecond = secondSize * 0.5f;

        bool overlapX = firstPosition.x - halfFirst.x < secondPosition.x + halfSecond.x && 
                         firstPosition.x + halfFirst.x > secondPosition.x - halfSecond.x;
    
        bool overlapY = firstPosition.y - halfFirst.y < secondPosition.y + halfSecond.y && 
                         firstPosition.y + halfFirst.y > secondPosition.y - halfSecond.y;

        return overlapX && overlapY;
    }

    public static bool IsInsideCircle(Vector2 point, Vector2 circleCenter, float radius)
    {
        float distSqr = (point - circleCenter).sqrMagnitude;
        return distSqr <= (radius * radius);
    }

    public static bool IsInsideCircle(float distSqr, float radius) => distSqr <= (radius * radius);
}
