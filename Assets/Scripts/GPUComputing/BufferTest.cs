using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class BufferTest : MonoBehaviour
{
    [SerializeField]
    private ComputeShader theShader;

    private ComputeBuffer dataBuffer;
    private ComputeBuffer argsBuffer;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        // Need to know the max size
        int maxSize = 10000;
        dataBuffer = new ComputeBuffer(maxSize, (sizeof(float) * 4), ComputeBufferType.Append);

        argsBuffer = new ComputeBuffer(1, (sizeof(int)));
        argsBuffer.SetData(new int[1]);

        int kernel = theShader.FindKernel("CSMain");
        theShader.SetBuffer(kernel, "dataBuffer", dataBuffer);
        theShader.SetBuffer(kernel, "argsBuffer", argsBuffer);
        theShader.Dispatch(kernel, 1, 1, 1);

        int[] size = new int[1];
        argsBuffer.GetData(size);

        ComputeData[] data = new ComputeData[size[0]];
        dataBuffer.GetData(data);

        stopwatch.Stop();
        UnityEngine.Debug.LogFormat("Execution Finished: {0} Data Sets Created In {1}ms", size[0], stopwatch.ElapsedMilliseconds.ToString());
    }
}


[StructLayout(LayoutKind.Sequential)] // Required to make the struct blittable
public struct ComputeData
{
    Quaternion q;
}