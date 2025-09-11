using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HoneyChunk : MonoBehaviour
{
    public float voxelSize = 1f;

    public Vector3Int voxelsPerAxis;
    public Bounds bounds;

    private Collider[] colliders;

    [HideInInspector]
    public Mesh mesh;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    public UnityEngine.Vector4[] densityValues;

    // Call after bounds are set
    public void SetUp(Material mat)
    {
        // detect overlapping colliders
        colliders = Physics.OverlapBox(center: bounds.center, halfExtents: bounds.extents);
        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
                Debug.Log($"Found an object: {c.gameObject.name}");
        }

        UnityEngine.Vector3 size = bounds.size;
        voxelsPerAxis[0] = Mathf.FloorToInt(size.x / voxelSize);
        voxelsPerAxis[1] = Mathf.FloorToInt(size.y / voxelSize); // voxels are only above yCutoff
        voxelsPerAxis[2] = Mathf.FloorToInt(size.z / voxelSize);

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        if (meshCollider.sharedMesh == null)
        {
            meshCollider.sharedMesh = mesh;
        }
        // force update
        meshCollider.enabled = false;
        meshCollider.enabled = true;

        meshRenderer.material = mat;

        // Initialize densityValues array based on the number of voxels
        int numVoxels = voxelsPerAxis.x * voxelsPerAxis.y * voxelsPerAxis.z;
        densityValues = new Vector4[numVoxels];
    }

    // Update is called once per frame
    public void UpdateHoneyGrowth() { }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        if (densityValues == null || densityValues.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;

        for (int i = 0; i < densityValues.Length; i++)
        {
            float density = densityValues[i].w;
            if (density > 0)
            {
                UnityEngine.Vector3 pos = new(
                    densityValues[i].x,
                    densityValues[i].y,
                    densityValues[i].z
                );
                Gizmos.DrawSphere(pos, density * 0.1f);
            }
        }
    }

    public void OnApplicationQuit()
    {
        DestroyImmediate(gameObject, false);
    }
}
