using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HoneyGenerator : MonoBehaviour
{
    public Vector2 xRange = new(-50f, 50f);
    public Vector2 yRange = new(0f, 50f);
    public Vector2 zRange = new(-10f, 10f);

    private Vector3 worldMin;
    private Vector3 worldMax;

    [Range(0f, 50f)]
    public float yCutoff = 5f; // Below this y value, individual voxels for honey will be culled.

    public float isoLevel = 0.9f;

    private Vector3 chunkSize = new(20, 20, 20);
    private List<HoneyChunk> chunks;

    public float voxelSize = 0.25f;

    private float nextUpdateTime = 0.0f;
    public float updateInterval = 0.1f;

    public BaseDensityGenerator densityGenerator;
    public GameObject chunkHolder;
    public ComputeShader marchingCubesShader;
    public ComputeShader signedDistanceFieldShader;
    public Material honeyMat;

    public bool debugMode = true;

    // Buffers
    private ComputeBuffer countBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer indexBuffer;

    private ComputeBuffer triangulationTableBuffer;

    void Awake()
    {
        Init();

        DestroyAllChunks();
    }

    void Start()
    {
        CreateAllChunks();

        PrepareBuffers();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > nextUpdateTime && chunks.Count != 0)
        {
            PrepareBuffers();

            nextUpdateTime = Time.time + updateInterval;

            foreach (HoneyChunk c in chunks)
            {
                // Debug.Log($"Updating {c.name}. Next action time is {nextUpdateTime}...");
                UpdateChunkMesh(c);
            }
        }
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void Init()
    {
        if (debugMode)
        {
            voxelSize = Mathf.Max(2f, voxelSize); // in debug mode voxels are large
            updateInterval = Mathf.Max(1f, updateInterval); // in debug model set updateInterval high
        }

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
        if (signedDistanceFieldShader == null)
        {
            signedDistanceFieldShader =
                Resources.Load("Shaders/Compute/SignedDistanceField", typeof(ComputeShader))
                as ComputeShader;
        }
    }

    private void CreateAllChunks()
    {
        for (float x = xRange[0]; x < xRange[1]; x += chunkSize[0])
        {
            for (float y = yRange[0]; y < yRange[1]; y += chunkSize[0])
            {
                for (float z = zRange[0]; z < zRange[1]; z += chunkSize[2])
                {
                    CreateChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    // find all HoneyChunk objects in scene and destroy
    private void DestroyAllChunks()
    {
        HoneyChunk[] oldChunks = FindObjectsByType<HoneyChunk>(FindObjectsSortMode.None);

        for (int i = 0; i < oldChunks.Length; i++)
        {
            Destroy(oldChunks[i].gameObject);
        }

        chunks = new List<HoneyChunk>();
    }

    private void CreateChunk(Vector3 minBound)
    {
        Vector3 maxBound = new(
            Mathf.Min(minBound[0] + chunkSize[0], xRange[1]),
            Mathf.Min(minBound[1] + chunkSize[1], yRange[1]),
            Mathf.Min(minBound[2] + chunkSize[2], zRange[1])
        );

        Bounds newChunkBounds = new();
        newChunkBounds.SetMinMax(minBound, maxBound);

        GameObject _newChunkObj = new($"Chunk {newChunkBounds.center}");

        _newChunkObj.transform.parent = chunkHolder.transform;
        HoneyChunk newChunk = _newChunkObj.AddComponent<HoneyChunk>();
        newChunk.bounds = newChunkBounds;

        newChunk.voxelSize = voxelSize;

        newChunk.SetUp(honeyMat, debugMode);

        chunks.Add(newChunk);
    }

    private void ScaleBySDF(HoneyChunk chunk)
    {
        if (chunk.colliderVertices == null || chunk.colliderIndices == null)
        {
            return;
        }

        signedDistanceFieldShader.SetBuffer(0, "points", pointsBuffer);
        signedDistanceFieldShader.SetBuffer(0, "colliderVertices", chunk.colliderVertices);
        signedDistanceFieldShader.SetBuffer(0, "colliderIndices", chunk.colliderIndices);

        signedDistanceFieldShader.SetVector("voxelsPerAxis", (Vector3)chunk.voxelsPerAxis);

        Vector3 blockSize = Common.GetBlockSize(signedDistanceFieldShader, "SDF");
        Vector3Int gridSize = new(
            Mathf.CeilToInt(chunk.voxelsPerAxis[0] / blockSize.x),
            Mathf.CeilToInt(chunk.voxelsPerAxis[1] / blockSize.y),
            Mathf.CeilToInt(chunk.voxelsPerAxis[2] / blockSize.z)
        );

        signedDistanceFieldShader.Dispatch(0, gridSize.x, gridSize.y, gridSize.z);
    }

    private void UpdateChunkMesh(HoneyChunk chunk)
    {
        worldMin = new(xRange[0], yRange[0], zRange[0]);
        worldMax = new(xRange[1], yRange[1], zRange[1]);

        pointsBuffer = densityGenerator.Generate(
            pointsBuffer,
            chunk.voxelsPerAxis,
            worldMin,
            worldMax,
            chunk.bounds.size,
            chunk.bounds.center,
            chunk.voxelSize
        );

        ScaleBySDF(chunk);

        if (debugMode)
        { // store density values within chunk for debugging
            pointsBuffer.GetData(chunk.densityValues);
        }

        indexBuffer.SetCounterValue(0);
        marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
        marchingCubesShader.SetBuffer(0, "triangles", indexBuffer);
        marchingCubesShader.SetVector("voxelsPerAxis", (Vector3)chunk.voxelsPerAxis);
        marchingCubesShader.SetFloat("isoLevel", isoLevel);

        marchingCubesShader.SetBuffer(0, "triangulationTable", triangulationTableBuffer);

        Vector3 blockSize = Common.GetBlockSize(marchingCubesShader, "March");
        Vector3Int gridSize = new(
            Mathf.CeilToInt(chunk.voxelsPerAxis[0] / blockSize.x),
            Mathf.CeilToInt(chunk.voxelsPerAxis[1] / blockSize.y),
            Mathf.CeilToInt(chunk.voxelsPerAxis[2] / blockSize.z)
        );

        marchingCubesShader.Dispatch(0, gridSize[0], gridSize[1], gridSize[2]);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(indexBuffer, countBuffer, 0);
        int[] triCountArray = { 0 };
        countBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        indexBuffer.GetData(tris, 0, 0, numTris);

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

        MeshCollider meshCollider = chunk.meshCollider;
        Mesh mesh = chunk.meshFilter.sharedMesh;
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;
    }

    void PrepareBuffers()
    {
        // first chunk has largest size possible
        Vector3Int targetVoxelsPerAxis = chunks[0].voxelsPerAxis;
        int numVoxels = targetVoxelsPerAxis.x * targetVoxelsPerAxis.y * targetVoxelsPerAxis.z;

        int maxTriangleCount = numVoxels * 5;

        // only create if points buffer is null or if size of first chunk has changed
        if (pointsBuffer == null || numVoxels != pointsBuffer.count)
        {
            ReleaseBuffers();

            indexBuffer = new ComputeBuffer(
                maxTriangleCount,
                sizeof(float) * 3 * 3,
                ComputeBufferType.Append
            );

            pointsBuffer = new ComputeBuffer(numVoxels, sizeof(float) * 4);
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
        if (indexBuffer != null)
        {
            pointsBuffer.Release();
            indexBuffer.Release();
            countBuffer.Release();
            pointsBuffer = null;
            indexBuffer = null;
            countBuffer = null;
        }

        triangulationTableBuffer?.Release();
        triangulationTableBuffer = null;
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
