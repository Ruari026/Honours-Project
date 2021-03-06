using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class TreeGeneratorComputeVer : MonoBehaviour
{
    [Header("Tree Data Generation Details")]
    [Range(0, 90)]
    [SerializeField]
    private float branchAngleMax = 0;
    [Range(0, 90)]
    [SerializeField]
    private float branchAngleMin = 0;

    [SerializeField]
    private uint numberOfBranchSplitsMax = 2;
    [SerializeField]
    private uint numberOfBranchSplitsMin = 2;

    [SerializeField]
    private float branchSizeMax = 1;
    [SerializeField]
    private float branchSizeMin = 1;
    [SerializeField]
    private AnimationCurve branchSizeCurve;

    [SerializeField]
    private uint numberOfIterations = 1;
    private uint currentIteration = 0;

    [SerializeField]
    private ComputeShader theShader = null;
    private ComputeBuffer dataBuffer = null;
    private ComputeBuffer argsBuffer = null;
    private BranchDataGPUVer[] theTree;

    [Header("Model Representation Generation Details")]
    [SerializeField]
    private GameObject branchModelPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        SetupBufferData();

        DispatchComputeShader();

        StartModelGeneration(theTree[0]);

        stopwatch.Stop();
        UnityEngine.Debug.LogFormat("GPU Algorithm Complete: {0}ms", stopwatch.ElapsedMilliseconds.ToString());
    }


    /*
    ============================================================================================================================================================================================================================================================================================================
    Traditional CPU Implementation of Fractal Tree Generation Algorithm
    ============================================================================================================================================================================================================================================================================================================
    */
    /// <summary>
    /// 
    /// </summary>
    private void SetupBufferData()
    {
        // When creating a buffer for the compute shader the size of the buffer needs to be pre defined as a shader cannot manipulate the size of the buffer during execution
        // Calculating the max number of required branches
        int bufferMaxCount = 0;
        for (int i = 1; i <= numberOfIterations; i++)
        {
            int addition = (int)(Mathf.Pow(numberOfBranchSplitsMax, (i - 1)));
            bufferMaxCount += (addition);
        }
        // Calculating the size of the elements in the data struct (in bytes)
        int bufferStride = 0;
        BranchDataGPUVer testData = new BranchDataGPUVer((int)numberOfBranchSplitsMax);
        bufferStride = Marshal.SizeOf(testData);
        dataBuffer = new ComputeBuffer(bufferMaxCount, bufferStride, ComputeBufferType.Append);


        // Also creating buffer to store the actual number of branches created in the compute shader
        argsBuffer = new ComputeBuffer(1, sizeof(int));
        argsBuffer.SetData(new int[] { 0 });

        //UnityEngine.Debug.LogFormat("Tree Data Setup, created {0} branches", maxPossibleBranches.ToString());
    }


    /// <summary>
    /// 
    /// </summary>
    private void DispatchComputeShader()
    {
        int shaderKernel = theShader.FindKernel("CSMain");

        // Binding the buffers to the shader
        theShader.SetBuffer(shaderKernel, "dataBuffer", dataBuffer);
        theShader.SetBuffer(shaderKernel, "argsBuffer", argsBuffer);

        // Executing compute shader to handle the procedural generation
        theShader.Dispatch(shaderKernel, 1, 1, 1);

        // Waiting for the shader to finish executing
        // Getting the generated output from the buffers
        int[] size = new int[1];
        argsBuffer.GetData(size);
        theTree = new BranchDataGPUVer[size[0]];
        dataBuffer.GetData(theTree);
    }


    /*
    ============================================================================================================================================================================================================================================================================================================
    Creating Visual Representation of each generated tree
    ============================================================================================================================================================================================================================================================================================================
    */
    private void StartModelGeneration(BranchDataGPUVer treeStart)
    {
        GameObject trunkModel = Instantiate(branchModelPrefab, this.transform);
        trunkModel.isStatic = true;
        //treeStart.spawnedPrefab = trunkModel;

        ContinueModelGeneration(treeStart, trunkModel);
    }

    private void ContinueModelGeneration(BranchDataGPUVer previousData, GameObject previousPrefab)
    {
        // Gets the previous branch's endpoint for positioning the next set of branch offshoots
        Transform endPoint = null;
        for (int i = 0; i < previousPrefab.transform.childCount; i++)
        {
            if (previousPrefab.transform.GetChild(i).tag == "EndPoint")
            {
                endPoint = previousPrefab.transform.GetChild(i);
            }
        }

        // Spawning in the next set of branch offshoots
        for (int i = 0; i < previousData.numberofChildBranches; i++)
        {
            BranchDataGPUVer nextData = theTree[previousData.childBranchesIdx[i]];

            // Creating model
            GameObject nextBranchPrefab = Instantiate(branchModelPrefab, this.transform);
            nextBranchPrefab.isStatic = true;
            //nextData.spawnedPrefab = nextBranchPrefab;

            // Positioning & Rotating model
            nextBranchPrefab.transform.position = endPoint.transform.position;
            nextBranchPrefab.transform.rotation = endPoint.transform.rotation * new Quaternion(nextData.rotation.x, nextData.rotation.y, nextData.rotation.z, nextData.rotation.w);

            // Sizing Model
            nextBranchPrefab.transform.localScale = (Vector3.one * nextData.scale);

            // Continuing Tree Generation
            ContinueModelGeneration(nextData, nextBranchPrefab);
        }
    }
}