using System.Collections.Generic;
using UnityEngine;

public class ObstacleDensity : BaseDensityGenerator
{
    void Awake()
    {
        densityShader =
            Resources.Load("Shaders/Compute/ObstacleDensity", typeof(ComputeShader))
            as ComputeShader;
        if (densityShader == null)
        {
            Debug.LogError("Failed to load ObstacleDensity compute shader!");
        }
    }

    public ComputeBuffer Generate(
        ComputeBuffer pointsBuffer,
        Vector3 voxelsPerAxis,
        Vector3 worldMin,
        Vector3 worldMax,
        Vector3 chunkSize,
        Vector3 chunkCenter,
        float voxelSize,
        float radius,
        float noiseScale
    )
    {
        buffersToRelease = new List<ComputeBuffer>();

        float time = Time.time;

        densityShader.SetFloat("time", time);
        densityShader.SetFloat("radius", radius);
        densityShader.SetFloat("noiseScale", noiseScale);

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
