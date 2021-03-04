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
    
    private ComputeBuffer treeBuffer = null;
    private BranchDataGPUVer[] theTree;

    [SerializeField]
    private ComputeShader theShader = null;

    [Header("Model Representation Generation Details")]
    [SerializeField]
    private GameObject branchModelPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        SetupGenerationData();

        GenerateTree();

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
    private void SetupGenerationData()
    {
        // When creating a buffer for the compute shader the size of the buffer needs to be pre defined as a shader cannot manipulate the size of the buffer during execution
        // Calculating the max number of required branches
        int maxPossibleBranches = 0;
        for (int i = 1; i <= numberOfIterations; i++)
        {
            int addition = (int)(Mathf.Pow(numberOfBranchSplitsMax, (i - 1)));
            maxPossibleBranches += (addition);
        }

        theTree = new BranchDataGPUVer[maxPossibleBranches];
        for (int j = 0; j < maxPossibleBranches; j++)
        {
            theTree[j] = new BranchDataGPUVer();
        }
        UnityEngine.Debug.LogFormat("Tree Data Setup, created {0} branches", maxPossibleBranches.ToString());
    }


    /// <summary>
    /// 
    /// </summary>
    private void GenerateTree()
    {
        // Creating buffer from previously generated data
        treeBuffer = new ComputeBuffer(theTree.Length, Marshal.SizeOf(typeof(BranchDataGPUVer)));
        treeBuffer.SetData(theTree);

        // Executing compute shader to handle the procedural generation
        int shaderKernel = theShader.FindKernel("CSMain");
        theShader.SetBuffer(shaderKernel, "theTree", treeBuffer);
        theShader.Dispatch(shaderKernel, 1, 1, 1);

        // Waiting for the shader to finish executing
        treeBuffer.GetData(theTree);
    }


    /*
    ============================================================================================================================================================================================================================================================================================================
    Creating Visual Representation of each generated tree
    ============================================================================================================================================================================================================================================================================================================
    */
    private void StartModelGeneration(BranchDataGPUVer treeStart)
    {
        // Checking that a tree has actually been generated
        if (treeStart != null)
        {
            GameObject trunkModel = Instantiate(branchModelPrefab, this.transform);
            trunkModel.isStatic = true;
            //treeStart.spawnedPrefab = trunkModel;

            ContinueModelGeneration(treeStart, trunkModel);
        }
        else
        {
            UnityEngine.Debug.LogError("ERROR: Tree Data has not been generated");
        }
    }

    private void ContinueModelGeneration(BranchDataGPUVer previousData, GameObject previousPrefab)
    {
        if (previousData != null && previousPrefab != null)
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
            for (int i = 0; i < previousData.numberOfChildBranches; i++)
            {
                BranchDataGPUVer nextData = theTree[previousData.childBranchesIdx[i]];

                // Creating model
                GameObject nextBranchPrefab = Instantiate(branchModelPrefab, this.transform);
                nextBranchPrefab.isStatic = true;
                //nextData.spawnedPrefab = nextBranchPrefab;

                // Positioning & Rotating model
                nextBranchPrefab.transform.position = endPoint.transform.position;
                nextBranchPrefab.transform.rotation = endPoint.transform.rotation * new Quaternion(nextData.branchRotation.x, nextData.branchRotation.y, nextData.branchRotation.z, nextData.branchRotation.w);

                // Sizing Model
                nextBranchPrefab.transform.localScale = (Vector3.one * nextData.branchScale);

                // Continuing Tree Generation
                ContinueModelGeneration(nextData, nextBranchPrefab);
            }
        }
    }
}