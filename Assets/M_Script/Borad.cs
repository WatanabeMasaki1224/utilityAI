using UnityEngine;

public class Borad
{
    public int width = 6;
    public int height = 6;
    public Unit[,] grid;

    public Borad()
    {
        grid = new Unit[width, height];
    }

    public bool IsInside(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    public bool IsEmpty(Vector2Int pos)
    {
        return grid[pos.x,pos.y] == null;
    }

    public void PlaceUnit(Unit unit)
    {
        grid[unit.position.x, unit.position.y] = unit;
    }

    public void MoveUnit(Unit unit,Vector2Int newPos)
    {
        grid[unit.position.x, unit.position.y] = null;
        unit.position = newPos;
        grid[newPos.x, newPos.y] = unit;
    }
}
