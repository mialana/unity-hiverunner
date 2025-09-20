using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class HoneyObstacle : MonoBehaviour
{
    public GameObject player;
    public float honeyAmount = 5f;

    // Mesh generation properties
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private ObstacleDensity densityGenerator;

    private HoneyGenerator honeyGenerator;

    // Marching cubes settings
    public float voxelSize = 0.25f;
    public Vector3 radius = new Vector3(5f, 5f, 0.5f);
    public Vector2 sizeRange = new(2, 5);

    public float noiseScale = 1f;
    public float noiseWeight = 0.5f;
    public float isoLevel = 0.5f;
    public Vector3Int voxelsPerAxis;
    public Material obstacleMaterial;

    // Compute buffers - similar to HoneyGenerator
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer indexBuffer;
    private ComputeBuffer countBuffer;
    private ComputeBuffer triangulationTableBuffer;
    private ComputeShader marchingCubesShader;

    public Vector4[] densityValues;

    public bool debugMode = false;

    void Awake()
    {
        // Get components
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        honeyGenerator = GameObject.Find("HoneyGenerator").GetComponent<HoneyGenerator>();

        densityGenerator = GameObject.Find("ObstacleDensity").GetComponent<ObstacleDensity>();

        // Load the marching cubes shader
        marchingCubesShader =
            Resources.Load("Shaders/Compute/MarchingCubes", typeof(ComputeShader)) as ComputeShader;

        // Create mesh
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.name = "HoneyObstacleMesh";
            meshFilter.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        float randomRadius = UnityEngine.Random.Range(sizeRange.x, sizeRange.y);
        radius.x = randomRadius;
        radius.y = randomRadius;

        voxelsPerAxis[0] = Mathf.CeilToInt(radius.x / voxelSize) + 1;
        voxelsPerAxis[1] = Mathf.CeilToInt(radius.y / voxelSize) + 1;
        voxelsPerAxis[2] = Mathf.CeilToInt(radius.z / voxelSize) + 1;

        honeyAmount = randomRadius;

        if (debugMode)
        { // Initialize densityValues array based Range.on the number of voxels
            int totalVoxels = voxelsPerAxis.x * voxelsPerAxis.y * voxelsPerAxis.z;
            densityValues = new Vector4[totalVoxels];
        }
    }

    public void Init()
    {
        meshRenderer.material = obstacleMaterial;

        GenerateMesh();
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject == player)
        {
            honeyGenerator.averageHoneyLevel -= honeyAmount;
            Destroy(gameObject);

            Debug.Log($"Honey Subtracted by {honeyAmount}!");
        }
    }

    public void GenerateMesh()
    {
        PrepareBuffers();

        Vector3 chunkSize = radius * 2;
        Vector3 chunkCenter = transform.position;

        pointsBuffer = densityGenerator.Generate(
            pointsBuffer,
            voxelsPerAxis,
            chunkCenter - chunkSize / 2,
            chunkCenter + chunkSize / 2,
            chunkSize,
            chunkCenter,
            voxelSize,
            radius,
            noiseScale,
            noiseWeight
        );

        if (debugMode)
        { // store density values within chunk for debugging
            pointsBuffer.GetData(densityValues);
        }

        // Run marching cubes
        indexBuffer.SetCounterValue(0);
        marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
        marchingCubesShader.SetBuffer(0, "triangles", indexBuffer);
        marchingCubesShader.SetVector("voxelsPerAxis", (Vector3)voxelsPerAxis);
        marchingCubesShader.SetFloat("isoLevel", isoLevel);
        marchingCubesShader.SetBuffer(0, "triangulationTable", triangulationTableBuffer);

        Vector3 blockSize = Common.GetBlockSize(marchingCubesShader, "March");
        Vector3Int gridSize = new(
            Mathf.CeilToInt(voxelsPerAxis.x / blockSize.x),
            Mathf.CeilToInt(voxelsPerAxis.y / blockSize.y),
            Mathf.CeilToInt(voxelsPerAxis.z / blockSize.z)
        );

        marchingCubesShader.Dispatch(0, gridSize.x, gridSize.y, gridSize.z);

        // Get triangle count
        ComputeBuffer.CopyCount(indexBuffer, countBuffer, 0);
        int[] triCountArray = { 0 };
        countBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        if (numTris < 1)
        {
            return;
        }

        // Get triangle data
        Triangle[] tris = new Triangle[numTris];
        indexBuffer.GetData(tris, 0, 0, numTris);

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = transform.InverseTransformPoint(tris[i][j]);
            }
        }

        // Update mesh
        Mesh mesh = meshFilter.sharedMesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;
        mesh.RecalculateNormals();

        // Update collider
        meshCollider.sharedMesh = mesh;
    }

    void PrepareBuffers()
    {
        int numVoxels = voxelsPerAxis.x * voxelsPerAxis.y * voxelsPerAxis.z;
        int maxTriangleCount = numVoxels * 5;

        if (pointsBuffer == null)
        {
            pointsBuffer = new ComputeBuffer(numVoxels, sizeof(float) * 4);
            indexBuffer = new ComputeBuffer(
                maxTriangleCount,
                sizeof(float) * 3 * 3,
                ComputeBufferType.Append
            );
            countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }

        if (triangulationTableBuffer == null)
        {
            int[] triangulationData = MarchTable.Triangulation;
            triangulationTableBuffer = new ComputeBuffer(triangulationData.Length, sizeof(int));
            triangulationTableBuffer.SetData(triangulationData);
        }
    }

    void ReleaseBuffers()
    {
        if (pointsBuffer != null)
        {
            pointsBuffer.Release();
            indexBuffer.Release();
            countBuffer.Release();
            triangulationTableBuffer.Release();

            pointsBuffer = null;
            indexBuffer = null;
            countBuffer = null;
            triangulationTableBuffer = null;
        }
    }

    struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (debugMode)
        {
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
    }
}
