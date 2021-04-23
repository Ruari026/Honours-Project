using System.Diagnostics;
using System.Runtime.InteropServices;

using UnityEngine;

public class TreeGeneratorComputeVer : TreeGeneratorBase
{
    private int maxPossibleBranches = 0;
    
    // Tree Data Generation Details
    [Header("Compute Shader Specific Details")]
    [SerializeField]
    private ComputeShader theShader = null;

    private ComputeBuffer dataBuffer = null;
    private ComputeBuffer connectionsBuffer = null;
    private ComputeBuffer argsBuffer = null;

    private BranchDataGPUVer[] theTree = null;
    private int[] treeConnections = null;

    /*
    ====================================================================================================
    TreeGeneratorBase Inherited Methods
    ====================================================================================================
    */
    #region TreeGeneratorBase Inherited Methods
    /// <summary>
    /// 
    /// </summary>
    public override void ResetData()
    {
        dataBuffer = null;

        connectionsBuffer = null;

        argsBuffer = null;

        theTree = null;

        treeConnections = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="debug"></param>
    /// <returns></returns>
    public override long GenerateTreeData()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        SetupBufferData();

        DispatchComputeShader();

        stopwatch.Stop();

        if (debug)
        {
            UnityEngine.Debug.LogFormat("GPU (Compute Shader) Algorithm Complete: {0}ms", stopwatch.ElapsedMilliseconds.ToString());
        }

        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 
    /// </summary>
    public override void GenerateTreeModels()
    {
        StartModelGeneration();
    }
    #endregion


    /*
    ====================================================================================================
    Traditional CPU Implementation of Fractal Tree Generation Algorithm
    ====================================================================================================
    */
    #region Traditional CPU Implementation of Fractal Tree Generation Algorithm
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

        // Calculating the size of the elements in the required structs (in bytes)
        int branchDataSize = Marshal.SizeOf(new BranchDataGPUVer((int)numberOfBranchSplits)); ; 
        int intSize = sizeof(int);

        // Creating buffer to store the main tree branch datas
        if (debug)
        {
            UnityEngine.Debug.LogFormat("Created Data Buffer (Count: {0}, Stride: {1}, Bytes: {2}", bufferMaxCount, branchDataSize, ((long)bufferMaxCount * branchDataSize));
        }
        dataBuffer = new ComputeBuffer(bufferMaxCount, branchDataSize);
        dataBuffer.SetData(new BranchDataGPUVer[bufferMaxCount]);

        // Creating buffer to store the connections between the branches
        if (debug)
        {
            UnityEngine.Debug.LogFormat("Created Connections Buffer (Count: {0}, Stride: {1}, Bytes: {2}", bufferMaxCount, intSize, (bufferMaxCount * intSize));
        }
        connectionsBuffer = new ComputeBuffer(bufferMaxCount, (intSize));
        connectionsBuffer.SetData(new int[bufferMaxCount]);

        // Also creating buffer to store the actual number of branches created in the compute shader && storing the number of connections through all the branches
        if (debug)
        {
            UnityEngine.Debug.LogFormat("Created Args Buffer (Count: {0}, Stride: {1}, Bytes: {2}", 2, intSize, (2 * intSize));
        }
        argsBuffer = new ComputeBuffer(2, (intSize));
        argsBuffer.SetData(new int[2]);


        if (debug)
        {
            UnityEngine.Debug.LogFormat("Tree Data Setup, created {0} branches", maxPossibleBranches.ToString());
        }
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
    }
    #endregion


    /*
    ====================================================================================================
    Creating Visual Representation of each generated tree
    ====================================================================================================
    */
    #region Creating Visual Representation of each generated tree
    /// <summary>
    /// 
    /// </summary>
    private void StartModelGeneration()
    {
        for (int i = 0; i < numberOfGenerations; i++)
        {
            BranchDataGPUVer treeStart = theTree[maxPossibleBranches * i];

            GameObject trunkModel = Instantiate(branchModelPrefab, this.transform);
            trunkModel.isStatic = true;
            //treeStart.spawnedPrefab = trunkModel;

            trunkModel.transform.position = new Vector3(i * 10, 0, 0);

            ContinueModelGeneration(treeStart, trunkModel);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="previousData"></param>
    /// <param name="previousPrefab"></param>
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
    #endregion
}