using System.Collections.Generic;
using UnityEngine;

public class HiveGenerator : MonoBehaviour
{
    public Dictionary<Vector2Int, HiveCell> cells = new();

    public GameObject hiveCellPrefab;
    public int rows = 3; // how many rows to generate at start

    readonly float cellWidth = 22.5f; // horizontal spacing between neighbors in same row
    readonly float rowHeight = 5f; // vertical spacing between rows

    private GameObject cellHolder;

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
    void Update() { }

    void GenerateHive()
    {
        for (int r = 0; r < rows; r++)
        {
            int cols = ((r & 1) == 0) ? 3 : 4; // alternate 3 and 4 cells
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
        }
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
