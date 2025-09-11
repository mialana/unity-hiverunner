using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HoneyGenerator : MonoBehaviour
{
    private Vector2 xRange = new(-50f, 50f);
    private Vector2 yRange = new(0f, 50f);
    public Vector2 zRange = new(-10f, 10f);

    [Range(0f, 50f)]
    public float yCutoff = 5f; // Below this y value, individual voxels for honey will be culled.

    public float isoLevel = 8f;

    private Vector3 chunkSize = new(20, 10, 20);
    private List<HoneyChunk> chunks;

    private float nextUpdateTime = 0.0f;
    public float updateInterval = 3f;

    public BaseDensityGenerator densityGenerator;
    public GameObject chunkHolder;
    public ComputeShader marchingCubesShader;
    public Material honeyMat;

    // Buffers
    private ComputeBuffer countBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer indexBuffer;

    void Awake()
    {
        GenerateStaticHoneyMesh(); // for visual purposes in editor

        if (Application.isPlaying)
        {
            chunks = new List<HoneyChunk>();

            DestroyAllChunks();
        }
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
                Debug.LogWarning(chunks.Count);
                CreateBuffers();
            }
            else if (Time.time > nextUpdateTime && chunks.Count != 0)
            {
                nextUpdateTime = Time.time + updateInterval;

                foreach (HoneyChunk c in chunks)
                {
                    Debug.Log($"Updating {c.name}. Next action time is {nextUpdateTime}...");
                    UpdateChunkHoneyGrowth(c);
                }
            }
        }
        else
        {
            ReleaseBuffers();
        }
    }

    private void UpdateChunkHoneyGrowth(HoneyChunk chunk)
    {
        int numVoxelsPerAxis = chunk.voxelsPerAxis[0] - 1;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / 8f);

        densityGenerator.Generate(
            pointsBuffer,
            chunk.voxelsPerAxis,
            new(0, 0, 0),
            chunkSize,
            chunk.bounds.center,
            chunk.voxelSize
        );

        indexBuffer.SetCounterValue(0);
        marchingCubesShader.SetBuffer(0, "points", pointsBuffer);
        marchingCubesShader.SetBuffer(0, "triangles", indexBuffer);
        marchingCubesShader.SetInt("numPointsPerAxis", chunk.voxelsPerAxis[0]);
        marchingCubesShader.SetFloat("isoLevel", isoLevel);

        marchingCubesShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

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

                Debug.Log(vertices[i * 3 + j]);
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
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

    private void GetOrSetChunkHolder()
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
    }

    private void GetOrSetDensityGenerator()
    {
        if (densityGenerator == null)
        {
            if (GameObject.Find("HoneyDensity"))
            {
                chunkHolder = GameObject.Find("HoneyDensity");
            }
            else
            {
                chunkHolder = new GameObject("HoneyDensity");
            }
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
        pointsBuffer?.Release();
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
