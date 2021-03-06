﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
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

        // Need to know the count of the buffer and the stride (size in bytes) of each element in the buffer
        int count = 10000;
        var test = new ComputeData();
        int stride = Marshal.SizeOf(test);

        // Creating each buffer
        dataBuffer = new ComputeBuffer(count, Marshal.SizeOf(test), ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, (sizeof(int)));
        argsBuffer.SetData(new int[1]);

        // Binding the buffers to the shader and executing the shader
        int kernel = theShader.FindKernel("CSMain");
        theShader.SetBuffer(kernel,  nameof(dataBuffer), dataBuffer);
        theShader.SetBuffer(kernel, nameof(argsBuffer), argsBuffer);
        theShader.Dispatch(kernel, 1, 1, 1);


        int[] size = new int[1];
        argsBuffer.GetData(size);
        
        var data = new ComputeData[size[0]];
        dataBuffer.GetData(data);

        var v = data[0];    
        stopwatch.Stop();
        UnityEngine.Debug.LogFormat("Execution Finished: {0} Data Sets Created In {1}ms", size[0], stopwatch.ElapsedMilliseconds.ToString());
    }
}


[StructLayout(LayoutKind.Sequential)] // Required to make the struct blittable
public struct ComputeData
{
    public System.Int32[] q;
}