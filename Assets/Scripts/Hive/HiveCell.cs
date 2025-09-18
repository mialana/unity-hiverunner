using UnityEngine;

public class HiveCell : MonoBehaviour
{
    public int row;
    public int col;

    // Store children in clockwise order
    public GameObject[] walls; // size 6

    Material hiveMat;
    Material hiveMatBack;

    void Awake()
    {
        hiveMat = Resources.Load("Materials/HiveMat", typeof(Material)) as Material;
        hiveMatBack = Resources.Load("Materials/HiveMatBack", typeof(Material)) as Material;
    }

    public void Init(int r, int c)
    {
        row = r;
        col = c;

        walls = new GameObject[6];
        walls[0] = transform.Find("geo_up").gameObject;
        walls[1] = transform.Find("geo_upRight").gameObject;
        walls[2] = transform.Find("geo_downRight").gameObject;
        walls[3] = transform.Find("geo_down").gameObject;
        walls[4] = transform.Find("geo_downLeft").gameObject;
        walls[5] = transform.Find("geo_upLeft").gameObject;

        foreach (GameObject w in walls)
        {
            w.GetComponent<MeshRenderer>().material = hiveMat;
        }

        GameObject backWall = transform.Find("geo_back").gameObject;
        backWall.GetComponent<MeshRenderer>().material = hiveMatBack;
    }

    public void SetWall(int dir, bool active)
    {
        // dir 0..5, clockwise
        if (walls[dir] != null)
            walls[dir].SetActive(active);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }

    // Update is called once per frame
    void Update() { }
}
