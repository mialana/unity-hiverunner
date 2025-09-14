using UnityEngine;

public static class Common
{
    public static Vector3 GetBlockSize(ComputeShader shader, string kernelName)
    {
        int kernelHandle = shader.FindKernel(kernelName);

        uint blockSizeX,
            blockSizeY,
            blockSizeZ;

        shader.GetKernelThreadGroupSizes(
            kernelHandle,
            out blockSizeX,
            out blockSizeY,
            out blockSizeZ
        );

        return new Vector3(blockSizeX, blockSizeY, blockSizeZ);
    }
}
