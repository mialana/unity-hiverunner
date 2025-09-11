// Ported from https://github.com/SebLague/Marching-Cubes/blob/master/Assets/Scripts/Density/DensityGenerator.cs

using System.Collections.Generic;
using UnityEngine;

public abstract class BaseDensityGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;
    public ComputeShader densityShader;

    protected List<ComputeBuffer> buffersToRelease;

    public virtual ComputeBuffer Generate(
        ComputeBuffer pointsBuffer,
        Vector3 numVoxelsPerAxis,
        Vector3 worldBounds,
        Vector3 chunkSize,
        Vector3 chunkCenter,
        float voxelSize
    )
    {
        Vector3Int numThreadsPerAxis = new(
            Mathf.CeilToInt(numVoxelsPerAxis[0] / (float)threadGroupSize),
            Mathf.CeilToInt(numVoxelsPerAxis[1] / (float)threadGroupSize),
            Mathf.CeilToInt(numVoxelsPerAxis[3] / (float)threadGroupSize)
        );

        // Points buffer is populated inside shader with pos (xyz) + density (w).
        // Set paramaters
        densityShader.SetBuffer(0, "points", pointsBuffer);
        densityShader.SetVector("numVoxelsPerAxis", numVoxelsPerAxis);
        densityShader.SetVector("worldSize", worldBounds);
        densityShader.SetVector("chunkSize", chunkSize);
        densityShader.SetVector(
            "chunkCenter",
            new Vector4(chunkCenter.x, chunkCenter.y, chunkCenter.z)
        );
        densityShader.SetFloat("voxelSize", voxelSize);

        // Dispatch shader
        densityShader.Dispatch(0, numThreadsPerAxis[0], numThreadsPerAxis[1], numThreadsPerAxis[2]);

        if (buffersToRelease != null)
        {
            foreach (var b in buffersToRelease)
            {
                b.Release();
            }
        }

        // Return voxel data buffer
        return pointsBuffer;
    }
}
