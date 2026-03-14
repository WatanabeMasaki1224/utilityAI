using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.InputSystem;

enum GamePhase
{
    Planning,
    Move,
    Attack
}


public class TurnManager : MonoBehaviour
{
    public List<Unit> units = new List<Unit>();
    public Dictionary<Unit, ActionPlan> plans;
    Board board;
    GamePhase phase = GamePhase.Planning;
    public GameObject meleePrefab;
    public GameObject midPrefab;
    public GameObject longPrefab;

    private void Awake()
    {
        plans = new Dictionary<Unit, ActionPlan>();
        board = new Board();
    }

    public void Start()
    {
        UnitView[] views = FindObjectsByType<UnitView>(FindObjectsSortMode.None);

        foreach (var v in views)
        {
            Vector2Int pos = new Vector2Int(
                Mathf.RoundToInt(v.transform.position.x),
                Mathf.RoundToInt(v.transform.position.z)
            );

            Unit unit = new Unit(v.type, pos);
            unit.isEnemy = v.isEnemy;
            unit.view = v.gameObject;

            units.Add(unit);
            board.PlaceUnit(unit);
        }
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (phase == GamePhase.Planning)
            {
                StartTurn();
                phase = GamePhase.Move;
            }
            else if (phase == GamePhase.Move)
            {
                MovePhase();
                phase = GamePhase.Attack;
            }
            else if (phase == GamePhase.Attack)
            {
                AttackPhase();
                CleanupPhase();
                phase = GamePhase.Planning;
            }
        }
    }

    void StartTurn()
    {
        plans.Clear();

        foreach (Unit unit in units)
        {
            if (!unit.isAlive)
                continue;
            ActionPlan plan = GenerateAIPlan(unit);
            plans[unit] = plan;
        }
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
        foreach (var unit in units)
        {
            ActionPlan plan = plans[unit];
            Vector2Int target =plan.targetPosition;
            Debug.Log(unit.type + " Ç™ " + target + " Ç…à⁄ìÆÇµÇÊÇ§Ç∆ÇµÇƒÇ¢ÇÈ");
            if (!board.IsInside(target))
            {
                continue;
            }
            if (board.IsEmpty(target))
            {
                board.MoveUnit(unit, target);
            }
        }
    }

    void AttackPhase()
    {
        foreach(Unit unit in units)
        {
            Debug.Log(unit.type + " ÇÃçUåÇÉtÉFÅ[ÉY");
            if (!unit.isAlive)
                continue;

            ActionPlan plan = plans[unit];

            if (!plan.willAttack)
                continue;

            Attack(unit);
        }
    }

    void CleanupPhase()
    {
        foreach(Unit unit in units)
        {
            if (!unit.isAlive)
            {
                board.grid[unit.position.x, unit.position.y] = null;
                Debug.Log(unit.type + " died");
            }  
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="aiUnit"></param>
    /// <returns></returns>
    ActionPlan GenerateAIPlan(Unit aiUnit)
    {
        ActionPlan plan = new ActionPlan(aiUnit.position,false,aiUnit.maxCost);
        //ëOÇ…1É}ÉXà⁄ìÆ
        Vector2Int forward = new Vector2Int(aiUnit.position.x, aiUnit.position.y-1);
        if(board.IsInside(forward) && board.IsEmpty(forward))
        {
            plan.targetPosition = forward;
        }

        else
        {
            plan.targetPosition = aiUnit.position;

        }

        //çUåÇîªíË
        foreach (Unit enemy in units)
        {
            if (enemy == aiUnit || !enemy.isAlive)
            {
                continue;
            }

            if (enemy.isEnemy == aiUnit.isEnemy)
                continue;

            if (aiUnit.type == UnitType.Melee && Vector2Int.Distance(plan.targetPosition, enemy.position) <= 1)
            {
                plan.willAttack = true;
                break;
            }

            if(aiUnit.type == UnitType.MidRange && Vector2Int.Distance(plan.targetPosition,enemy.position)<= 2)
            {
                plan.willAttack = true;
                break;
            }

            if(aiUnit.type == UnitType.LongRange && plan.targetPosition.x == enemy.position.x)
            {
                plan.willAttack = true;
                break;
            }
        }
        plan.usedCost = aiUnit.maxCost;
        return plan;
    }

    void Attack(Unit attacker)
    {
        List<Unit> targets = new List<Unit>();

        //çUåÇâ¬î\Ç»ìGÇíTÇ∑
        foreach (Unit enemy in units)
        {
            if (enemy == attacker || !enemy.isAlive)
                continue;

            if (enemy.isEnemy == attacker.isEnemy)
                continue;

            float dist = Vector2Int.Distance(attacker.position, enemy.position);

            if (attacker.type == UnitType.Melee && dist <= 1)
                targets.Add(enemy);

            if (attacker.type == UnitType.MidRange && dist <= 2)
                targets.Add(enemy);

            if (attacker.type == UnitType.LongRange && attacker.position.x == enemy.position.x)
                targets.Add(enemy);
        }
        //çUåÇëŒè€Ç™Ç¢Ç»Ç¢
        if (targets.Count == 0)
            return;
        //É^Å[ÉQÉbÉgëIë
        Unit target = SelectTarget(targets);
        Damage(attacker, target);
        Debug.Log(attacker.type + " attacked " + target.type + " HP:" + target.hp);

    }

    Unit SelectTarget(List<Unit> targets)
    {
        Unit best = targets[0];
        foreach (Unit attackTarget in targets)
        {
            if(attackTarget.hp < best.hp)
                best = attackTarget;
        }
        return best;
    }

    void Damage(Unit attacker, Unit target)
    {
        int damage = 0;
        if (attacker.type == UnitType.Melee)
            damage = 3;
        if(attacker.type == UnitType.MidRange)
            damage = 2;
        if(attacker.type == UnitType.LongRange)
            damage = 4;
        target.hp -= damage;
    }
}
