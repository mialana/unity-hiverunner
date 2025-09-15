static const int blockSize = 8;

float3 interpolateVerts(float4 v1, float4 v2, float isoLevel) {
    float t = (isoLevel - v1.w) / (v2.w - v1.w);
    return v1.xyz + t * (v2.xyz - v1.xyz);
}

int indexFromCoord(int x, int y, int z, float3 voxelsPerAxis) {
    return x + y * voxelsPerAxis.x + z * voxelsPerAxis.x * voxelsPerAxis.y;
}

struct Triangle {
    float3 vertexC;
    float3 vertexB;
    float3 vertexA;
};

static const int cornerIndexAFromEdge[12] = {
    0,
    1,
    2,
    3,
    4,
    5,
    6,
    7,
    0,
    1,
    2,
    3
};

static const int cornerIndexBFromEdge[12] = {
    1,
    2,
    3,
    0,
    5,
    6,
    7,
    4,
    4,
    5,
    6,
    7
};