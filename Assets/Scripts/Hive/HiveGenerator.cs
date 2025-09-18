using System.Collections.Generic;
using UnityEngine;

public class HiveGenerator : MonoBehaviour
{
    public Dictionary<Vector2Int, HiveCell> cells = new();

    public GameObject player;

    public GameObject hiveCellPrefab;
    public int rows = 3; // how many rows to generate at start

    readonly float cellWidth = 11.25f; // horizontal spacing between neighbors in same row
    readonly float rowHeight = 10f; // vertical spacing between rows

    private GameObject cellHolder;

    public int cullDistance = 2; // rows below player to destroy
    public int bufferRows = 2; // how many rows ahead of player to generate

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

    void GenerateRow(int r)
    {
        if (activeRows.Contains(r))
            return;

        int cols = 7;
        for (int c = 0; c < cols; c++)
        {
            Vector3 pos = GetPosition(r, c);

            GameObject obj = Instantiate(
                hiveCellPrefab,
                pos,
                Quaternion.identity,
                cellHolder.transform
            );
            obj.name = $"Hive Cell ({r}, {c})";

            HiveCell cell = obj.GetComponent<HiveCell>() ?? obj.AddComponent<HiveCell>();
            cell.Init(r, c);

            cells[new Vector2Int(r, c)] = cell;
        }

        activeRows.Add(r);
        highestRow = Mathf.Max(highestRow, r);
    }

    Vector3 GetPosition(int r, int c)
    {
        // Base Y is determined only by row
        float baseY = 20f + r * rowHeight;

        // Spread 7 cells symmetrically: c=0 → -3*cellWidth, c=6 → +3*cellWidth
        float x = (c - 3) * cellWidth;
        float y = baseY;

        // Apply vertical stagger to columns 0,2,4,6
        if (c % 2 == 0)
        {
            y += 5f; // tweak multiplier for how tall the offset should be
        }

        return new Vector3(x, y, 0f);
    }
}
