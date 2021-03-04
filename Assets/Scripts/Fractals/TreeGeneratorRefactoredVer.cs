using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TreeGeneratorRefactoredVer : MonoBehaviour
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

    private BranchDataGPUVer[] theTree;

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
        // Info required to recursively generate the tree
        int createdBranches = 0;
        int branchesToCreate = 0;

        int workingIteration = 0;

        BranchDataGPUVer[] workingBranchTree = new BranchDataGPUVer[numberOfIterations];
        
        // Creating the start of the tree
        BranchDataGPUVer treeStart = theTree[createdBranches];
        workingBranchTree[0] = treeStart;
        // Handling branch size
        treeStart.branchScale = 1.0f;
        // Handling branch rotation
        treeStart.branchRotation = Quaternion.identity;
        // Setting up child branch connections
        treeStart.numberOfChildBranches = (int)numberOfBranchSplitsMax;
        branchesToCreate = (int)numberOfBranchSplitsMax;


        // Compute shaders don't support recursive methods so the algorithm needs to be restructured to work with a while loop
        bool isComplete = false;
        while (!isComplete)
        {
            // Checking if the tree has finished generating
            if ((createdBranches == (branchesToCreate)) && (workingIteration == (numberOfIterations - 1)))
            {
                isComplete = true;
            }
            // Otherwise gets the next branch to generate
            else
            {
                // If the current branch is at the end of the tree then need to backtrack till a unfinished branch is found
                if (workingIteration == (numberOfIterations - 1))
                {
                    bool suitableBranchFound = false;
                    while (!suitableBranchFound)
                    {
                        // Cleaning up working tree
                        //workingBranchTree[workingIteration] = NULL;

                        // Moving back up the tree
                        workingIteration--;
                        // Checking that if the backtracking has found no suitable branch to build from
                        if (workingIteration == -1)
                        {
                            //UnityEngine.Debug.LogError("ERROR: No suitable branch found to build from");
                            //UnityEngine.Debug.Break();
                        }
                        BranchDataGPUVer branchToCheck = workingBranchTree[workingIteration];

                        // Checking if that branch has had all of it's sub branches made
                        // If not then the generation can continue with that branch
                        if (branchToCheck.connectedChildBranches != branchToCheck.numberOfChildBranches)
                        {
                            suitableBranchFound = true;
                        }
                    }
                }

                // Then build off of the current branch
                BranchDataGPUVer previousBranch = workingBranchTree[workingIteration];

                // "Creating" new branch
                createdBranches++;
                BranchDataGPUVer newBranch = theTree[createdBranches];
                workingIteration++;
                workingBranchTree[workingIteration] = newBranch;

                // Setting the details of the new branch
                // Handling branch size
                newBranch.branchScale = 1.0f;
                // Handling branch rotation
                newBranch.branchRotation = Quaternion.Euler(branchAngleMax, ((360.0f / previousBranch.numberOfChildBranches) * previousBranch.connectedChildBranches), 0);

                // Checking if child branches should be added to the new branch
                if (workingIteration < (numberOfIterations - 1))
                {
                    newBranch.numberOfChildBranches = (int)numberOfBranchSplitsMax;
                    branchesToCreate += (int)numberOfBranchSplitsMax;
                }

                // Connecting the previous branch to the new branch
                int idx = previousBranch.connectedChildBranches;
                int[] mb = previousBranch.childBranchesIdx;
                mb[idx] = createdBranches;
                previousBranch.connectedChildBranches++;
            }
        }
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