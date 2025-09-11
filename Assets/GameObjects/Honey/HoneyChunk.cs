using System.Collections.Generic;
using UnityEngine;

public class HoneyChunk : MonoBehaviour
{
    public float voxelSize = 2f;

    public Vector3Int voxelsPerAxis;
    public Bounds bounds;

    private Collider[] colliders;

    // Call after bounds are set
    public void SetUp()
    {
        // detect overlapping colliders
        colliders = Physics.OverlapBox(center: bounds.center, halfExtents: bounds.extents);
        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
                Debug.Log($"Found an object: {c.gameObject.name}");
        }

        Vector3 size = bounds.size;
        voxelsPerAxis[0] = Mathf.FloorToInt(size.x / voxelSize);
        voxelsPerAxis[1] = Mathf.FloorToInt(size.y / voxelSize); // voxels are only above yCutoff
        voxelsPerAxis[2] = Mathf.FloorToInt(size.z / voxelSize);
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
