using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HoneyGenerator : MonoBehaviour
{
    public Vector2 xRange = new(-50f, 50f);
    public Vector2 yRange = new(0f, 50f);
    public Vector2 zRange = new(-10f, 10f);

    [Range(0f, 50f)]
    public float yCutoff = 5f; // Below this y value, individual voxels for honey will be culled.

    public float isoLevel = 0.9f;

    private Vector3 chunkSize = new(20, 20, 20);
    private List<HoneyChunk> chunks;

    private float nextUpdateTime = 0.0f;
    public float updateInterval = 0.01f;

    public BaseDensityGenerator densityGenerator;
    public GameObject chunkHolder;
    public ComputeShader marchingCubesShader;
    public Material honeyMat;

    // Buffers
    private ComputeBuffer countBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer indexBuffer;

    private ComputeBuffer edgeTableBuffer;
    private ComputeBuffer triTableBuffer;

    void Awake()
    {
        Init();

        if (Application.isPlaying)
        {
            chunks = new List<HoneyChunk>();

            DestroyAllChunks();

            CreateTableBuffers();
        }
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            CreateAllChunks();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            if (pointsBuffer == null)
            {
                CreateBuffers();
            }
            else if (Time.time > nextUpdateTime && chunks.Count != 0)
            {
                nextUpdateTime = Time.time + updateInterval;

                foreach (HoneyChunk c in chunks)
                {
                    // Debug.Log($"Updating {c.name}. Next action time is {nextUpdateTime}...");
                    UpdateChunkHoneyGrowth(c);
                }
            }
        }
        else
        {
            ReleaseBuffers();
            ReleaseTableBuffers();
        }
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    private void Init()
    {
        if (chunkHolder == null)
        {
            if (GameObject.Find("ChunkHolder"))
            {
                chunkHolder = GameObject.Find("ChunkHolder");
            }
            else
            {
                chunkHolder = new GameObject("ChunkHolder");
            }
        }
        if (honeyMat == null)
        {
            honeyMat = Resources.Load("Materials/HoneyMat", typeof(Material)) as Material;
        }
        if (densityGenerator == null)
        {
            if (GameObject.Find("HoneyDensity"))
            {
                densityGenerator = GameObject.Find("HoneyDensity").GetComponent<HoneyDensity>();
            }
            else
            {
                GameObject honeyDensityObj = new GameObject("HoneyDensity");
                densityGenerator = honeyDensityObj.AddComponent<HoneyDensity>();
            }
        }
        if (marchingCubesShader == null)
        {
            marchingCubesShader =
                Resources.Load("Shaders/Compute/MarchingCubes", typeof(ComputeShader))
                as ComputeShader;
        }

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.material = honeyMat;

        Mesh mesh = new() { name = "HoneyMesh" };
        filter.mesh = mesh;

        mesh.RecalculateNormals();
    }

    private void CreateAllChunks()
    {
        for (float x = xRange[0]; x < xRange[1]; x += chunkSize[0])
        {
            for (float z = zRange[0]; z < zRange[1]; z += chunkSize[2])
            {
                float y = yCutoff; // only need one row of y.
                CreateChunk(new Vector3(x, y, z));
            }
        }
    }

    private void CreateChunk(Vector3 minBound)
    {
        Vector3 maxBound = new(
            Mathf.Min(minBound[0] + chunkSize[0], xRange[1]),
            Mathf.Min(minBound[1] + chunkSize[1], yRange[1]),
            Mathf.Min(minBound[2] + chunkSize[2], yRange[1])
        );

        Bounds newChunkBounds = new();
        newChunkBounds.SetMinMax(minBound, maxBound);

        GameObject _newChunkObj = new($"Chunk {newChunkBounds.center}");

        _newChunkObj.transform.parent = chunkHolder.transform;
        HoneyChunk newChunk = _newChunkObj.AddComponent<HoneyChunk>();
        newChunk.bounds = newChunkBounds;

        newChunk.SetUp(honeyMat);

        chunks.Add(newChunk);
    }

    private void UpdateChunkHoneyGrowth(HoneyChunk chunk)
    {
        int numVoxelsPerAxis = chunk.voxelsPerAxis[0] - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / 8f);

        Vector3 worldMin = new(xRange[0], yCutoff, zRange[0]);
        Vector3 worldMax = new(xRange[1], yRange[1], zRange[1]);

        densityGenerator.Generate(
            pointsBuffer,
            chunk.voxelsPerAxis,
            worldMin,
            worldMax,
            chunkSize,
            chunk.bounds.center,
            chunk.voxelSize
        );

        indexBuffer.SetCounterValue(0);
        marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
        marchingCubesShader.SetBuffer(0, "triangles", indexBuffer);
        marchingCubesShader.SetInt("numPointsPerAxis", chunk.voxelsPerAxis[0]);
        marchingCubesShader.SetFloat("isoLevel", isoLevel);

        marchingCubesShader.SetBuffer(0, "edgeTable", edgeTableBuffer);
        marchingCubesShader.SetBuffer(0, "triTable", triTableBuffer);

        marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        pointsBuffer.GetData(chunk.densityValues);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(indexBuffer, countBuffer, 0);
        int[] triCountArray = { 0 };
        countBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        indexBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }

    // find all HoneyChunk objects in scene and destroy
    private void DestroyAllChunks()
    {
        var oldChunks = FindObjectsByType<HoneyChunk>(FindObjectsSortMode.None);

        for (int i = 0; i < oldChunks.Length; i++)
        {
            Destroy(oldChunks[i].gameObject);
        }
    }

    void CreateBuffers()
    {
        Vector3Int targetVoxelsPerAxis = chunks[0].voxelsPerAxis;
        int numPoints = targetVoxelsPerAxis.x * targetVoxelsPerAxis.y * targetVoxelsPerAxis.z;
        int numVoxelsPerAxis = targetVoxelsPerAxis.x - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        if (!Application.isPlaying || pointsBuffer == null || numPoints != pointsBuffer.count)
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }

            indexBuffer = new ComputeBuffer(
                maxTriangleCount,
                sizeof(float) * 3 * 3,
                ComputeBufferType.Append
            );

            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }
    }

    void ReleaseBuffers()
    {
        if (indexBuffer != null)
        {
            pointsBuffer.Release();
            indexBuffer.Release();
            countBuffer.Release();
            pointsBuffer = null;
            indexBuffer = null;
            countBuffer = null;
        }
    }

    void CreateTableBuffers()
    {
        // Edge table: 256 ints
        int[] edgeTableData = MarchTables.EdgeTable;
        edgeTableBuffer = new ComputeBuffer(edgeTableData.Length, sizeof(int));
        edgeTableBuffer.SetData(edgeTableData);

        // Tri table: 256 * 16 ints
        int[] triTableData = MarchTables.TriTable;
        triTableBuffer = new ComputeBuffer(triTableData.Length, sizeof(int));
        triTableBuffer.SetData(triTableData);
    }

    void ReleaseTableBuffers()
    {
        edgeTableBuffer?.Release();
        edgeTableBuffer = null;
        triTableBuffer?.Release();
        triTableBuffer = null;
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
}
