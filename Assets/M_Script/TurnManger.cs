using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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
    // プレイヤーが操作中のユニット
    Unit selectedUnit = null;
    Vector2Int? selectedUnitTarget = null;

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
        if (phase == GamePhase.Planning)
        {
            HandlePlayerPlanning();
            Debug.Log("プランターン");
        }
        else if (phase == GamePhase.Move)
        {
            ExecuteMovePhase();
            phase = GamePhase.Attack;
            Debug.Log("移動ターン");
        }
        else if (phase == GamePhase.Attack)
        {
            //ExecuteAttackPhase();
            CleanupPhase();
            phase = GamePhase.Planning;
            Debug.Log("攻撃ターン");
        }
    }

    void HandlePlayerPlanning()
    {
        // プレイヤーがユニットを選択して移動先をクリック
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            Vector2Int boardPos = WorldToBoardPos(worldPos);

            // 選択済みユニットがあれば移動
            if (selectedUnit != null)
            {
                if (board.IsInside(boardPos) && board.IsEmpty(boardPos))
                {
                    board.MoveUnit(selectedUnit, boardPos);
                    Debug.Log(selectedUnit.type + " will move to " + boardPos);
                    selectedUnit = null; // 移動完了
                }
            }
            else
            {
                // クリックした位置にプレイヤーユニットがいれば選択
                Unit unit = board.GetUnitAt(boardPos);
                if (unit != null && !unit.isEnemy)
                {
                    selectedUnit = unit;
                    Debug.Log(unit.type + " selected");
                }
            }
        }

        // プレイヤーが操作完了したらスペースで Move フェイズへ
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            phase = GamePhase.Move;
        }
    }

    void ExecuteMovePhase()
    {
        foreach (var unit in units)
        {
            if (!unit.isAlive)
                continue;

            // プレイヤー操作済みならスキップ
            if (!unit.isEnemy && unit == selectedUnit)
                continue;

            // AIユニットは plans に従って移動
            if (plans.TryGetValue(unit, out ActionPlan plan))
            {
                Vector2Int target = plan.targetPosition;

                if (board.IsInside(target) && board.IsEmpty(target))
                {
                    board.MoveUnit(unit, target);
                    Debug.Log(unit.type + " moved to " + target);
                }
            }
        }

        // Move フェイズ終了後は選択解除
        selectedUnit = null;
    }

    Vector2Int WorldToBoardPos(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + centerOffset.x);
        int y = Mathf.RoundToInt(worldPos.y + centerOffset.y);
        return new Vector2Int(x, y);
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
        Attack(UnitType.Melee, 1);
        CleanupPhase();
        Attack(UnitType.MidRange, 2);
        CleanupPhase();
        Attack(UnitType.LongRange, -1);
        CleanupPhase();
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

    void Attack(UnitType type,int range)
    {
        //攻撃ペアのリスト
        List<(Unit attacker, Unit target)> attacks = new List<(Unit,Unit)>();
        //攻撃対象を探す
        foreach (Unit attackerUnit in units)
        {
            if(!attackerUnit.isAlive || attackerUnit.type != type) 
                continue;

            List<Unit> possibleTargets =  new List<Unit>();
            foreach(Unit enemy in units)
            {
                if(enemy == attackerUnit || !enemy.isAlive || enemy.isEnemy == attackerUnit.isEnemy)
                    continue;

                float dist =Vector2Int.Distance(attackerUnit.position,enemy.position);
                if(attackerUnit.type == UnitType.Melee && dist <= 1)
                    possibleTargets.Add(enemy);
                if(attackerUnit.type == UnitType.MidRange && dist <= 2 )
                    possibleTargets.Add(enemy);
                if(enemy.type == UnitType.LongRange && attackerUnit.position.x == enemy.position.x)
                    possibleTargets.Add(enemy);
            }
            if (possibleTargets.Count == 0)
                continue;

            Unit taege = SelectTarget(possibleTargets);
            attacks.Add((attackerUnit, taege));
        }

        foreach (var attack in attacks)
        {
            Damage(attack.attacker, attack.target);
            Debug.Log(attack.attacker.type + " attacked " + attack.target.type + " HP:" + attack.target.hp);
        }

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
