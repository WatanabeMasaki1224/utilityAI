using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public List<Unit> units = new List<Unit>();
    public Dictionary<Unit, ActionPlan> plans;

    public void Start()
    {
        plans = new Dictionary<Unit, ActionPlan>();
    }

    void StartTurn()
    {
        
    }

    public void ExecuteTurn()
    {
        MovePhase();
        AttackPhase();
        CleanupPhase();
    }

    void EndTurn()
    {

    }

    void MovePhase()
    {

    }

    void AttackPhase()
    {

    }

    void CleanupPhase()
    {

    }
}
