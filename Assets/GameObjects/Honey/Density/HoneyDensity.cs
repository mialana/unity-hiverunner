using System.Collections.Generic;
using UnityEngine;

public class HoneyDensity : BaseDensityGenerator
{
    public override ComputeBuffer Generate(
        ComputeBuffer pointsBuffer,
        Vector3 voxelsPerAxis,
        Vector3 worldMin,
        Vector3 worldMax,
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
            voxelsPerAxis,
            worldMin,
            worldMax,
            chunkSize,
            chunkCenter,
            voxelSize
        );
    }
}
