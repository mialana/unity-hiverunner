using System.Collections.Generic;
using UnityEngine;

public class HoneyChunk : MonoBehaviour
{
    public float voxelSize = 2f;

    private List<HoneyVoxel> voxels;
    private Vector3 voxelsPerAxis;

    public Bounds bounds;

    Collider[] colliders;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // voxelsPerAxis[0] = Mathf.FloorToInt((xRange[1] - xRange[0]) / voxelSize);
        // voxelsPerAxis[1] = Mathf.FloorToInt((yRange[1] - yCutoff) / voxelSize); // voxels are only above yCutoff
        // voxelsPerAxis[2] = Mathf.FloorToInt((zRange[1] - zRange[0]) / voxelSize);
    }

    public void SetUp()
    {
        // detect overlapping colliders
        colliders = Physics.OverlapBox(center: bounds.center, halfExtents: bounds.extents);
        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
                Debug.Log($"Found an object: {c.gameObject.name}");
        }
    }

    // Update is called once per frame
    public void UpdateHoneyGrowth() { }

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
