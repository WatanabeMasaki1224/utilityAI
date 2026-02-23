using UnityEngine;
using static UnityEditor.PlayerSettings;
using UnityEngine.UIElements;

public enum UnitType
{
    Melee,
    MidRange,
    LongRange
}


public class Unit 
{
    public UnitType type;
    public Vector2Int position;
    public int hp = 6;
    public int maxCost = 5;
    public int currentCost;
    public bool isAlive => hp > 0;

    public Unit(UnitType type, Vector2Int startPos)
    {
        this.type = type;
        position = startPos;
        currentCost = maxCost;
    }
}
