using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HoneyGenerator : MonoBehaviour
{
    public GameObject player;

    public Vector2 xRange = new(-50f, 50f);
    public Vector2 yRange = new(0f, 50f);
    public Vector2 zRange = new(-10f, 10f);

    private Vector3 worldMin;
    private Vector3 worldMax;

    public float isoLevel = 0.5f;

    private Vector3 chunkSize = new(20, 20, 20);
    private Dictionary<Vector2Int, HoneyChunk> chunks;

    public float voxelSize = 0.25f;

    private float nextUpdateTime = 0.0f;
    public float updateInterval = 0.1f;

    public HoneyDensity densityGenerator;
    public GameObject chunkHolder;
    public ComputeShader marchingCubesShader;
    public ComputeShader signedDistanceFieldShader;
    public ComputeShader honeyObstacleDensityShader;
    public Material honeyMat;

    public bool debugMode = true;

    // Buffers
    private ComputeBuffer countBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer indexBuffer;

    private ComputeBuffer triangulationTableBuffer;

    private int currentBottomRow;
    private int currentTopRow;
    public int visibleRows = 3; // how many rows to keep at once

    public float honeyRiseRate = 0.002f;
    public float minLevelBelowPlayer = 15f;
    public float averageHoneyLevel = 0f;

    void Awake()
    {
        Init();
        DestroyAllChunks();
    }

    void Start()
    {
        chunks = new Dictionary<Vector2Int, HoneyChunk>();
        currentTopRow = 0;

        CreateInitialChunks();

        PrepareBuffers();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time < 5f)
        { // delay honey rising for a bit
            return;
        }
        if (player.transform.position.y - averageHoneyLevel > minLevelBelowPlayer)
        {
            averageHoneyLevel += 0.1f; // speed up until reaches min level
        }
        else
        {
            averageHoneyLevel += honeyRiseRate;
        }

        if (Time.time > nextUpdateTime && chunks.Count != 0)
        {
            PrepareBuffers();

            nextUpdateTime = Time.time + updateInterval;

            int playerRow = Mathf.FloorToInt(player.transform.position.y / chunkSize.y);

            // Always ensure we have exactly `visibleRows` rows
            int minRow = playerRow - 1; // keep a row below player
            int maxRow = minRow + visibleRows - 1; // and extend upwards

            // Add rows above
            while (currentTopRow < maxRow)
            {
                CreateRow(++currentTopRow);
            }

            // Remove rows below
            while (currentBottomRow < minRow)
            {
                RemoveRow(currentBottomRow++);
            }

            foreach (HoneyChunk c in chunks.Values)
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
                GameObject honeyDensityObj = new("HoneyDensity");
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
        if (honeyObstacleDensityShader == null)
        {
            honeyObstacleDensityShader =
                Resources.Load("Shaders/Compute/HoneyObstacleDensity", typeof(ComputeShader))
                as ComputeShader;
        }
    }

    private void CreateInitialChunks()
    {
        currentBottomRow = 0;
        for (int i = 0; i < visibleRows; i++)
        {
            CreateRow(i);
            currentTopRow = i;
        }
    }

    private void CreateRow(int rowIndex)
    {
        int numX = Mathf.FloorToInt((xRange.y - xRange.x) / chunkSize.x);

        for (int xi = 0; xi < numX; xi++)
        {
            Vector2Int coord = new(xi, rowIndex);

            if (!chunks.ContainsKey(coord))
            {
                CreateChunk(coord);
            }
        }
    }

    private void RemoveRow(int rowIndex)
    {
        int numX = Mathf.FloorToInt((xRange.y - xRange.x) / chunkSize.x);

        for (int xi = 0; xi < numX; xi++)
        {
            Vector2Int coord = new(xi, rowIndex);
            if (chunks.TryGetValue(coord, out HoneyChunk chunk))
            {
                Destroy(chunk.gameObject);
                chunks.Remove(coord);
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

        chunks = new Dictionary<Vector2Int, HoneyChunk>();
    }

    private void CreateChunk(Vector2Int coord)
    {
        Vector3 minBound = CoordToWorldMin(coord);
        Vector3 maxBound = minBound + chunkSize;

        Bounds newChunkBounds = new();
        newChunkBounds.SetMinMax(minBound, maxBound);

        GameObject _newChunkObj = new($"Chunk {newChunkBounds.center}");

        _newChunkObj.transform.parent = chunkHolder.transform;
        HoneyChunk newChunk = _newChunkObj.AddComponent<HoneyChunk>();
        newChunk.bounds = newChunkBounds;

        newChunk.voxelSize = voxelSize;

        newChunk.SetUp(player, honeyMat, debugMode);

        chunks[coord] = newChunk;
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
        float worldMinY = chunks.First().Value.bounds.min.y;
        float worldMaxY = chunks.Last().Value.bounds.max.y;

        worldMin = new(xRange[0], worldMinY, zRange[0]);
        worldMax = new(xRange[1], worldMaxY, zRange[1]);

        pointsBuffer = densityGenerator.Generate(
            pointsBuffer,
            chunk.voxelsPerAxis,
            worldMin,
            worldMax,
            chunk.bounds.size,
            chunk.bounds.center,
            chunk.voxelSize,
            averageHoneyLevel
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

        if (numTris < 1)
        {
            return;
        }

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
        Vector3Int targetVoxelsPerAxis = chunks.First().Value.voxelsPerAxis;
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

    private Vector3 CoordToWorldMin(Vector2Int coord)
    {
        float x = xRange.x + coord.x * chunkSize.x;
        float y = coord.y * chunkSize.y;
        float z = zRange.x; // since you only have 1 chunk in z

        return new Vector3(x, y, z);
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
