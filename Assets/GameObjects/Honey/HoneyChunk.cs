using System.Collections.Generic;
using UnityEngine;

public class HoneyChunk : MonoBehaviour
{
    public float voxelSize = 2f;

    private List<HoneyVoxel> voxels;
    private Vector3 voxelsPerAxis;

    public Bounds bounds;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // voxelsPerAxis[0] = Mathf.FloorToInt((xRange[1] - xRange[0]) / voxelSize);
        // voxelsPerAxis[1] = Mathf.FloorToInt((yRange[1] - yCutoff) / voxelSize); // voxels are only above yCutoff
        // voxelsPerAxis[2] = Mathf.FloorToInt((zRange[1] - zRange[0]) / voxelSize);
    }

    // Update is called once per frame
    void Update() { }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    public void OnApplicationQuit()
    {
        DestroyImmediate(gameObject, false);
    }
}
