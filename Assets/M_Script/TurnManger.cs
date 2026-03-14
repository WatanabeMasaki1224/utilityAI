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
    int boardWidth = 6;
    int boardHeight = 6;
    Vector2Int centerOffset;

    private void Awake()
    {
        plans = new Dictionary<Unit, ActionPlan>();
        board = new Board();
        centerOffset = new Vector2Int(boardWidth / 2, boardHeight / 2);
    }

    public void Start()
    {
        CreateUnit(UnitType.Melee, new Vector2Int(2, 0), false, meleePrefab);
        CreateUnit(UnitType.MidRange, new Vector2Int(3, 0), false, midPrefab);
        CreateUnit(UnitType.LongRange, new Vector2Int(4, 0), false, longPrefab);

        CreateUnit(UnitType.Melee, new Vector2Int(2, 5), true, meleePrefab);
        CreateUnit(UnitType.MidRange, new Vector2Int(3, 5), true, midPrefab);
        CreateUnit(UnitType.LongRange, new Vector2Int(4, 5), true, longPrefab);
    }

    void CreateUnit(UnitType type, Vector2Int boardPos, bool isEnemy, GameObject prefab)
    {
        Unit unit = new Unit(type, boardPos);
        unit.isEnemy = isEnemy;

        GameObject obj = Instantiate(prefab);

        // シーン上の位置だけ中央オフセットで補正
        Vector3 scenePos = new Vector3(
            boardPos.x - centerOffset.x,
            boardPos.y - centerOffset.y,
            0
        );
        obj.transform.position = scenePos;

        UnitView view = obj.GetComponent<UnitView>();
        unit.hp = view.hp;
        unit.damage = view.damage;
        unit.view = obj;

        units.Add(unit);
        board.PlaceUnit(unit);
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
            Debug.Log(unit.type + " が " + target + " に移動しようとしている");
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
            Debug.Log(unit.type + " の攻撃フェーズ");
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
        foreach (Unit unit in units)
        {
            if (!unit.isAlive)
            {
                // ボードから削除
                board.grid[unit.position.x, unit.position.y] = null;

                // GameObject が残っていれば消す
                if (unit.view != null)
                {
                    Destroy(unit.view);
                    unit.view = null;
                }

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
        //前に1マス移動
        Vector2Int forward = new Vector2Int(aiUnit.position.x, aiUnit.position.y-1);
        if(board.IsInside(forward) && board.IsEmpty(forward))
        {
            plan.targetPosition = forward;
        }

        else
        {
            plan.targetPosition = aiUnit.position;

        }

        //攻撃判定
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

        //攻撃可能な敵を探す
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
        //攻撃対象がいない
        if (targets.Count == 0)
            return;
        //ターゲット選択
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
        target.hp -= attacker.damage;
    }
}
