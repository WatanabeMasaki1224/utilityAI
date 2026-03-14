using UnityEngine;

public class ActionPlan 
{
    public Vector2Int targetPosition;
    public bool willAttack;
    public int usedCost;

    public ActionPlan(Vector2Int pos, bool attack, int cost)
    {
        targetPosition = pos;
        willAttack = attack;
        usedCost = cost;
    }
}
