using System.Collections.Generic;
using UnityEngine;

public class HiveGenerator : MonoBehaviour
{
    public Dictionary<Vector2Int, HiveCell> cells = new();

    public GameObject player;

    public GameObject hiveCellPrefab;
    public int rows = 3; // how many rows to generate at start

    readonly float cellWidth = 22.5f; // horizontal spacing between neighbors in same row
    readonly float rowHeight = 5f; // vertical spacing between rows

    private GameObject cellHolder;

    public int cullDistance = 3; // rows below player to destroy
    public int bufferRows = 3; // how many rows ahead of player to generate

    int highestRow = -1; // keep track of the top row generated
    HashSet<int> activeRows = new();

    void Awake()
    {
        if (cellHolder == null)
        {
            if (GameObject.Find("CellHolder"))
            {
                cellHolder = GameObject.Find("CellHolder");
            }
            else
            {
                cellHolder = new GameObject("CellHolder");
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateHive();
    }

    // Update is called once per frame
    void Update()
    {
        int playerRow = Mathf.RoundToInt((player.transform.position.y - 20f) / rowHeight);

        // Cull entire rows
        int minRow = playerRow - cullDistance;
        List<int> rowsToDestroy = new();
        foreach (int r in activeRows)
        {
            if (r < minRow)
                rowsToDestroy.Add(r);
        }
        foreach (int r in rowsToDestroy)
            DestroyRow(r);

        // Generate rows ahead
        int targetTop = playerRow + bufferRows;
        for (int r = highestRow + 1; r <= targetTop; r++)
        {
            GenerateRow(r);
        }
    }

    void GenerateHive()
    {
        for (int r = 0; r < rows; r++)
        {
            GenerateRow(r);
        }
    }

    void GenerateRow(int r)
    {
        if (activeRows.Contains(r))
            return; // already exists

        int cols = (r % 2 == 0) ? 3 : 4;
        for (int c = 0; c < cols; c++)
        {
            Vector3 pos = GetPosition(r, c);

            GameObject _newHiveCell = Instantiate(
                hiveCellPrefab,
                pos,
                Quaternion.identity,
                cellHolder.transform
            );
            _newHiveCell.name = $"Hive Cell ({r}, {c})";

            HiveCell newCell = _newHiveCell.AddComponent<HiveCell>();

            newCell.Init(r, c);

            cells[new Vector2Int(r, c)] = newCell;
        }

        activeRows.Add(r);
        highestRow = Mathf.Max(highestRow, r);
    }

    void DestroyRow(int r)
    {
        if (!activeRows.Contains(r))
            return;

        List<Vector2Int> toRemove = new();
        foreach (var kvp in cells)
        {
            if (kvp.Value.row == r)
            {
                Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
            cells.Remove(key);

        activeRows.Remove(r);
    }

    Vector3 GetPosition(int r, int c)
    {
        float y = 20f + r * rowHeight; // 20 is base Y for row 0
        float x;

        if (r % 2 == 0)
        {
            // Even row (3 cells, centered at -22.5, 0, 22.5)
            x = -cellWidth + c * cellWidth; // c = 0..2
        }
        else
        {
            // Odd row (4 cells, centered at -33.75, -11.25, 11.25, 33.75)
            x = -1.5f * cellWidth + c * cellWidth; // c = 0..3
        }

        return new Vector3(x, y, 0f);
    }
}
