using System.Collections.Generic;
using UnityEngine;

public class HoneyDensity : BaseDensityGenerator
{
    public override ComputeBuffer Generate(
        ComputeBuffer pointsBuffer,
        Vector3 numVoxelsPerAxis,
        Vector3 worldBounds,
        Vector3 chunkSize,
        Vector3 chunkCenter,
        float voxelSize
    )
    {
        buffersToRelease = new List<ComputeBuffer>();

        float time = Time.time;

        densityShader.SetFloat("time", time);

        return base.Generate(
            pointsBuffer,
            numVoxelsPerAxis,
            worldBounds,
            chunkSize,
            chunkCenter,
            voxelSize
        );
    }
}
