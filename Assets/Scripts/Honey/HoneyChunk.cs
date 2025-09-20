using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HoneyChunk : MonoBehaviour
{
    public float voxelSize = 0.5f;

    public Vector3Int voxelsPerAxis;
    public Bounds bounds;

    private Collider[] colliders;
    public ComputeBuffer colliderVertices;
    public ComputeBuffer colliderIndices;

    [HideInInspector]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    public Vector4[] densityValues;

    public List<GameObject> honeyObstacles = new();
    public GameObject honeyObstaclePrefab;
    public GameObject player;

    public Vector2 obstacleZRange;

    void Awake()
    {
        Bounds hiveGeneratorBounds = GameObject
            .Find("HiveGenerator")
            .GetComponent<HiveGenerator>()
            .bounds;
        obstacleZRange = new(hiveGeneratorBounds.min.z, hiveGeneratorBounds.max.z);
    }

    // Call after bounds are set
    public void SetUp(GameObject player, Material mat, bool debugMode)
    {
        this.player = player;

        Vector3 size = bounds.size;
        voxelsPerAxis[0] = Mathf.CeilToInt(size.x / voxelSize) + 1;
        voxelsPerAxis[1] = Mathf.CeilToInt(size.y / voxelSize) + 1;
        voxelsPerAxis[2] = Mathf.CeilToInt(size.z / voxelSize) + 1;

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

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = $"Chunk Mesh {bounds.center}";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh; // set up mesh in only mesh filter
        }

        meshRenderer.material = mat;

        FindChunkColliders();

        if (debugMode)
        {
            // Initialize densityValues array based on the number of voxels
            int totalVoxels = voxelsPerAxis.x * voxelsPerAxis.y * voxelsPerAxis.z;
            densityValues = new Vector4[totalVoxels];
        }
    }

    // Update is called once per frame
    private void FindChunkColliders()
    {
        // detect overlapping colliders
        colliders = Physics.OverlapBox(center: bounds.center, halfExtents: bounds.extents);
        if (colliders.Length != 0)
        {
            BuildComputeBuffers();
        }
    }

    private void BuildComputeBuffers()
    {
        List<Vector3> allVertices = new();
        List<int> allIndices = new();
        int vertexOffset = 0;

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.name == "ant")
            {
                continue;
            }

            if (collider.gameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                Mesh colliderMesh = meshFilter.sharedMesh;
                Vector3[] vertices = (Vector3[])colliderMesh.vertices.Clone();
                collider.gameObject.transform.TransformPoints(vertices);

                allVertices.AddRange(vertices);

                // Offset indices by the current vertex count.
                foreach (var index in colliderMesh.triangles)
                {
                    allIndices.Add(index + vertexOffset);
                }

                vertexOffset += vertices.Length;
            }
        }

        // Create and populate compute buffers
        if (colliderVertices != null)
            colliderVertices.Release();
        if (colliderIndices != null)
            colliderIndices.Release();

        if (allVertices.Count > 0)
        {
            colliderVertices = new ComputeBuffer(allVertices.Count, sizeof(float) * 3);
            colliderVertices.SetData(allVertices);

            colliderIndices = new ComputeBuffer(allIndices.Count, sizeof(int));
            colliderIndices.SetData(allIndices);
        }

        GenerateObstacles();
    }

    private void ReleaseComputeBuffers()
    {
        if (colliderVertices != null)
        {
            colliderVertices.Release();
            colliderVertices = null;
        }

        if (colliderIndices != null)
        {
            colliderIndices.Release();
            colliderIndices = null;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        Vector3 gizmosSize = bounds.size;
        gizmosSize.z = obstacleZRange.y - obstacleZRange.x;

        Gizmos.DrawWireCube(bounds.center, gizmosSize);

        if (densityValues == null || densityValues.Length == 0)
        {
            return;
        }

        // Draw spheres to debug density

        Color gizmosColor = Color.black;

        for (int i = 0; i < densityValues.Length; i++)
        {
            float density = densityValues[i].w;

            gizmosColor.r = density;
            gizmosColor.g = density;
            gizmosColor.b = density;

            Gizmos.color = gizmosColor;
            Vector3 pos = new(densityValues[i].x, densityValues[i].y, densityValues[i].z);
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }

    private void OnDestroy()
    {
        ReleaseComputeBuffers();
    }

    public void OnApplicationQuit()
    {
        DestroyImmediate(gameObject, false);
    }

    public void GenerateObstacles()
    {
        int obstacleCount = 3;

        Material obstacleMaterial =
            Resources.Load("Materials/HoneyObstacleMat", typeof(Material)) as Material;

        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 randomPos = new(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(obstacleZRange.x, obstacleZRange.y)
            );
            GameObject obstacleObj = new();
            obstacleObj.name = $"Honey Obstacle {i}";

            obstacleObj.transform.SetLocalPositionAndRotation(randomPos, Quaternion.identity);
            obstacleObj.transform.parent = transform;

            honeyObstacles.Add(obstacleObj);

            HoneyObstacle obstacle = obstacleObj.AddComponent<HoneyObstacle>();
            obstacle.player = player;
            obstacle.obstacleMaterial = obstacleMaterial;

            obstacle.Init();
        }
    }
}
