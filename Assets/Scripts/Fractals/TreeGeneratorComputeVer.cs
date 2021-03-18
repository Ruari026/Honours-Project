using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

public class TreeGeneratorComputeVer : MonoBehaviour
{
    // Tree Data Generation Details
    [Header("Tree Data Generation Details (NEEDS TO MATCH THE VALUES DEFINED IN THE COMPUTE SHADER)")]
    [Min(1)]
    [SerializeField]
    private uint numberOfGenerations = 1;
    private int maxPossibleBranches = 0;

    [Range(0, 90)]
    [SerializeField]
    private float branchAngle = 0;

    [SerializeField]
    private uint numberOfBranchSplits = 2;

    [SerializeField]
    private float branchSize = 1;

    [SerializeField]
    private uint numberOfIterations = 1;
    private uint currentIteration = 0;

    private int spawnedBranches = 0;


    // Tree Data Generation Details
    [Header("Compute Shader Details")]
    [SerializeField]
    private ComputeShader theShader = null;

    private ComputeBuffer dataBuffer = null;
    private ComputeBuffer connectionsBuffer = null;
    private ComputeBuffer argsBuffer = null;

    private BranchDataGPUVer[] theTree;
    private int[] treeConnections;


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

        stopwatch.Stop();
        UnityEngine.Debug.LogFormat("GPU (Compute Shader) Algorithm Complete: {0}ms", stopwatch.ElapsedMilliseconds.ToString());

        //StartModelGeneration();
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
            int addition = (int)(Mathf.Pow(numberOfBranchSplits, (i - 1)));
            bufferMaxCount += (addition);
        }
        maxPossibleBranches = bufferMaxCount;
        bufferMaxCount *= (int)numberOfGenerations;

        // Calculating the size of the elements in the data struct (in bytes)
        int bufferStride = 0;
        BranchDataGPUVer testData = new BranchDataGPUVer((int)numberOfBranchSplits);
        bufferStride = Marshal.SizeOf(testData);
        dataBuffer = new ComputeBuffer(bufferMaxCount, bufferStride);
        dataBuffer.SetData(new BranchDataGPUVer[bufferMaxCount]);

        // Creating buffer to store the connections between the branches
        connectionsBuffer = new ComputeBuffer(bufferMaxCount, (sizeof(int)));
        connectionsBuffer.SetData(new int[bufferMaxCount]);

        // Also creating buffer to store the actual number of branches created in the compute shader && storing the number of connections through all the branches
        argsBuffer = new ComputeBuffer(2, (sizeof(int)));
        argsBuffer.SetData(new int[2]);

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
        theShader.SetBuffer(shaderKernel, "connectionsBuffer", connectionsBuffer);
        theShader.SetBuffer(shaderKernel, "argsBuffer", argsBuffer);

        // Executing compute shader to handle the procedural generation
        theShader.Dispatch(shaderKernel, 1, 1, 1);

        // Waiting for the shader to finish executing
        // Getting the generated output from the buffers
        int[] size = new int[2];
        argsBuffer.GetData(size);
        argsBuffer.Release();

        theTree = new BranchDataGPUVer[size[0]];
        theTree = new BranchDataGPUVer[maxPossibleBranches * numberOfGenerations];
        dataBuffer.GetData(theTree);
        dataBuffer.Release();

        treeConnections = new int[size[1]];
        treeConnections = new int[maxPossibleBranches * numberOfGenerations];
        connectionsBuffer.GetData(treeConnections);
        connectionsBuffer.Release();

        bool b = true;
    }


    /*
    ============================================================================================================================================================================================================================================================================================================
    Creating Visual Representation of each generated tree
    ============================================================================================================================================================================================================================================================================================================
    */
    private void StartModelGeneration()
    {
        for (int i = 0; i < numberOfGenerations; i++)
        {
            if (maxPossibleBranches * i >= theTree.Length)
            {
                bool b = true;
            }
            BranchDataGPUVer treeStart = theTree[maxPossibleBranches * i];

            GameObject trunkModel = Instantiate(branchModelPrefab, this.transform);
            trunkModel.isStatic = true;
            //treeStart.spawnedPrefab = trunkModel;

            trunkModel.transform.position = new Vector3(i * 10, 0, 0);

            ContinueModelGeneration(treeStart, trunkModel);
        }
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
            BranchDataGPUVer nextData = theTree[treeConnections[previousData.childBranchesStartIdx + i]];

            // Creating model
            GameObject nextBranchPrefab = Instantiate(branchModelPrefab, this.transform);
            nextBranchPrefab.isStatic = true;
            //nextData.spawnedPrefab = nextBranchPrefab;

            // Positioning & Rotating model
            nextBranchPrefab.transform.position = endPoint.transform.position;
            Quaternion newRotation = Quaternion.Euler(nextData.rotation);
            nextBranchPrefab.transform.rotation = endPoint.transform.rotation * newRotation;
            nextBranchPrefab.transform.parent = previousPrefab.transform;

            // Sizing Model
            nextBranchPrefab.transform.localScale = (Vector3.one * nextData.scale);

            // Continuing Tree Generation
            ContinueModelGeneration(nextData, nextBranchPrefab);
        }
    }
}