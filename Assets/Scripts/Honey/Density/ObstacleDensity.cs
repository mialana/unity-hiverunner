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
        Vector3 radius,
        float noiseScale,
        float noiseWeight
    )
    {
        buffersToRelease = new List<ComputeBuffer>();

        float time = Time.time;

        densityShader.SetFloat("time", time);
        densityShader.SetVector("radius", radius);
        densityShader.SetFloat("noiseScale", noiseScale);
        densityShader.SetFloat("noiseWeight", noiseWeight);

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
