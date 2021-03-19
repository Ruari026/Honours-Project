using System.Diagnostics;
using UnityEngine;

public class TreeGeneratorRefactoredVer : TreeGeneratorBase
{
    // Arrays that contain the info passed by buffers
    private BranchDataRefactoredVer[] branchData = null;
    private int[] branchConnections = null;
    private int maxPossibleBranches = 0;


    /*
    ============================================================================================================================================================================================================================================================================================================
    TreeGeneratorBase Inherited Methods
    ============================================================================================================================================================================================================================================================================================================
    */
    /// <summary>
    /// 
    /// </summary>
    public override void ResetData()
    {
        branchData = null;

        branchConnections = null;

        maxPossibleBranches = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="debug"></param>
    /// <returns></returns>
    public override long GenerateTreeData(bool debug)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        SetupGenerationData();

        for (int i = 0; i < numberOfGenerations; i++)
        {
            GenerateTreeDatas(i);
        }

        stopwatch.Stop();

        if (debug)
        {
            UnityEngine.Debug.LogFormat("CPU (GPU Emulation) Algorithm Complete: {0}ms", stopwatch.ElapsedMilliseconds.ToString());
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
        // Calculating the max number of required branches & the max number of branch connections
        maxPossibleBranches = 0;
        for (int i = 1; i <= numberOfIterations; i++)
        {
            int addition = (int)(Mathf.Pow(numberOfBranchSplits, (i - 1)));
            maxPossibleBranches += (addition);
        }

        // Setting up the arrays that emulate the compute shader buffers
        branchData = new BranchDataRefactoredVer[maxPossibleBranches * numberOfGenerations];
        branchConnections = new int[maxPossibleBranches * numberOfGenerations];

        // Setting default values for every element on both the branch data container and the branch connections container
        for (int i = 0; i < maxPossibleBranches * numberOfGenerations; i++)
        {
            branchData[i] = new BranchDataRefactoredVer();
            branchConnections[i] = -1;
        }
        //UnityEngine.Debug.LogFormat("Tree Data Setup: setup {0} branch datas, setup {1} connection datas", branchData.Length.ToString(), branchConnections.Length.ToString());
    }


    /// <summary>
    /// 
    /// </summary>
    private void GenerateTreeDatas(int currentGeneration)
    {
        // Getting the start index for all info on the currentGeneration
        int currentGenStartIdx = (maxPossibleBranches * currentGeneration);

        // Info required to recursively generate the tree
        int createdBranches = 0;
        int branchesToCreate = 0;

        int workingIteration = 0;

        int[] workingBranchTree = new int[numberOfIterations];
        
        // Creating the start of the tree
        BranchDataRefactoredVer treeStart = new BranchDataRefactoredVer();

        // Handling branch size
        treeStart.branchScale = 1.0f;

        // Handling branch rotation
        treeStart.branchRotation = Quaternion.identity;

        // Setting up child branch connections
        treeStart.childBranchesStartIdx = currentGenStartIdx;
        treeStart.numberOfChildBranches = (int)numberOfBranchSplits;
        branchesToCreate = (int)numberOfBranchSplits;

        // Storing newly created branch
        workingBranchTree[0] = currentGenStartIdx + createdBranches;
        branchData[currentGenStartIdx + createdBranches] = treeStart;


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
                        int branchIdx = workingBranchTree[workingIteration];
                        BranchDataRefactoredVer branchToCheck = branchData[branchIdx];

                        // Checking if that branch has had all of it's sub branches made
                        // If not then the generation can continue with that branch
                        if (branchToCheck.connectedChildBranches != branchToCheck.numberOfChildBranches)
                        {
                            suitableBranchFound = true;
                        }
                    }
                }

                // Then build off of the current branch
                int previousBranchIdx = workingBranchTree[workingIteration];
                BranchDataRefactoredVer previousBranch = branchData[previousBranchIdx]; ;

                // Creating new branch
                createdBranches++;
                BranchDataRefactoredVer newBranch = new BranchDataRefactoredVer();
                workingIteration++;

                // Setting the details of the new branch
                // Handling branch size
                newBranch.branchScale = 1.0f;

                // Handling branch rotation
                newBranch.branchRotation = Quaternion.Euler(branchAngle, ((360.0f / previousBranch.numberOfChildBranches) * previousBranch.connectedChildBranches), 0);

                // Checking if child branches should be added to the new branch
                if (workingIteration < (numberOfIterations - 1))
                {
                    newBranch.numberOfChildBranches = (int)numberOfBranchSplits;
                    newBranch.childBranchesStartIdx = currentGenStartIdx + branchesToCreate;
                    branchesToCreate += (int)numberOfBranchSplits;
                }

                // Connecting the previous branch to the new branch
                int connectionIdx = (previousBranch.childBranchesStartIdx + previousBranch.connectedChildBranches);
                branchConnections[connectionIdx] = createdBranches;
                previousBranch.connectedChildBranches++;

                // Saving changed details on the old branch
                branchData[workingBranchTree[workingIteration - 1]] = previousBranch;

                // Saving new branch
                branchData[currentGenStartIdx + createdBranches] = newBranch;
                workingBranchTree[workingIteration] = currentGenStartIdx + createdBranches;
            }
        }
    }


    /*
    ============================================================================================================================================================================================================================================================================================================
    Creating Visual Representation of each generated tree
    ============================================================================================================================================================================================================================================================================================================
    */
    /// <summary>
    /// 
    /// </summary>
    private void StartModelGeneration()
    {
        for (int i = 0; i < numberOfGenerations; i++)
        {
            BranchDataRefactoredVer treeStart = branchData[maxPossibleBranches * i];

            GameObject trunkModel = Instantiate(branchModelPrefab, this.transform);
            trunkModel.isStatic = true;
            trunkModel.transform.position = new Vector3(i * 10, 0, 0);

            ContinueModelGeneration(treeStart, trunkModel);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="previousData"></param>
    /// <param name="previousPrefab"></param>
    private void ContinueModelGeneration(BranchDataRefactoredVer previousData, GameObject previousPrefab)
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
            BranchDataRefactoredVer nextData = branchData[branchConnections[previousData.childBranchesStartIdx + i]];

            // Creating model
            GameObject nextBranchPrefab = Instantiate(branchModelPrefab, this.transform);
            nextBranchPrefab.isStatic = true;

            // Positioning & Rotating model
            nextBranchPrefab.transform.position = endPoint.transform.position;
            nextBranchPrefab.transform.rotation = endPoint.transform.rotation * new Quaternion(nextData.branchRotation.x, nextData.branchRotation.y, nextData.branchRotation.z, nextData.branchRotation.w);
            nextBranchPrefab.transform.parent = previousPrefab.transform;

            // Sizing Model
            nextBranchPrefab.transform.localScale = (Vector3.one * nextData.branchScale);

            // Continuing Tree Generation
            ContinueModelGeneration(nextData, nextBranchPrefab);
        }
    }
}