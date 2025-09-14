// Ported from https://github.com/SebLague/Marching-Cubes/blob/master/Assets/Scripts/Density/DensityGenerator.cs

using System.Collections.Generic;
using UnityEngine;

public abstract class BaseDensityGenerator : MonoBehaviour
{
    public ComputeShader densityShader;

    protected List<ComputeBuffer> buffersToRelease;

    public virtual ComputeBuffer Generate(
        ComputeBuffer pointsBuffer,
        Vector3 voxelsPerAxis,
        Vector3 worldMin,
        Vector3 worldMax,
        Vector3 chunkSize,
        Vector3 chunkCenter,
        float voxelSize
    )
    {
        // Points buffer is populated inside shader with pos (xyz) + density (w).
        // Set paramaters
        densityShader.SetBuffer(0, "points", pointsBuffer);
        densityShader.SetVector("voxelsPerAxis", voxelsPerAxis);
        densityShader.SetVector("worldMin", worldMin);
        densityShader.SetVector("worldMax", worldMax);
        densityShader.SetVector("chunkSize", chunkSize);
        densityShader.SetVector(
            "chunkCenter",
            new Vector4(chunkCenter.x, chunkCenter.y, chunkCenter.z)
        );
        densityShader.SetFloat("voxelSize", voxelSize);

        // Dispatch shader
        Vector3 blockSize = Common.GetBlockSize(densityShader, "Density");

        Vector3Int gridSize = new(
            Mathf.CeilToInt(voxelsPerAxis[0] / blockSize.x),
            Mathf.CeilToInt(voxelsPerAxis[1] / blockSize.y),
            Mathf.CeilToInt(voxelsPerAxis[2] / blockSize.z)
        );

        densityShader.Dispatch(0, gridSize[0], gridSize[1], gridSize[2]);

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
