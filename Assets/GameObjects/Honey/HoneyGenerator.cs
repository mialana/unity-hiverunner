using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HoneyGenerator : MonoBehaviour
{
    private Vector2 xRange = new(-50f, 50f);
    private Vector2 yRange = new(0f, 50f);

    public Vector2 zRange = new(-10f, 10f);

    [Tooltip("Below this y value, individual voxels for honey will be culled.")]
    [Range(0f, 50f)]
    public float yCutoff = 5f;

    void OnEnable()
    {
        GenerateStaticHoneyMesh();
    }

    // Update is called once per frame
    void Update() { }

    private void GenerateStaticHoneyMesh()
    {
        if (!gameObject)
        {
            Debug.LogWarning("Honey Generator script needs to be attached to HoneyObject.");
            return;
        }

        gameObject.transform.localScale.Set(5f, 5f, 5f);

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        // Define cube vertices in LOCAL space (centered at origin, unit size)
        Vector3[] vertices = new Vector3[]
        {
            new(xRange[0], yRange[0], zRange[0]), // 0
            new(xRange[1], yRange[0], zRange[0]), // 1
            new(xRange[1], yCutoff, zRange[0]), // 2
            new(xRange[0], yCutoff, zRange[0]), // 3
            new(xRange[0], yRange[0], zRange[1]), // 4
            new(xRange[1], yRange[0], zRange[1]), // 5
            new(xRange[1], yCutoff, zRange[1]), // 6
            new(xRange[0], yCutoff, zRange[1]), // 7
        };
        // Triangles (12 total → 2 per face × 6 faces)
        // csharpier-ignore
        int[] triangles = new int[]
        {
            // back
            0, 2, 1, 0, 3, 2,
            // right
            1, 2, 6, 1, 6, 5,
            // front
            5, 6, 7, 5, 7, 4,
            // left
            4, 7, 3, 4, 3, 0,
            // top
            3, 7, 6, 3, 6, 2,
            // bottom
            4, 0, 1, 4, 1, 5
        };

        // Simple cube UVs
        Vector2[] uvs = new Vector2[]
        {
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(0, 1),
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(0, 1),
        };

        Mesh mesh = new()
        {
            name = "HoneyMesh",
            vertices = vertices,
            uv = uvs,
            triangles = triangles,
        };
        filter.mesh = mesh;

        mesh.RecalculateNormals();
    }
}
