using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;

public class TreeGeneratorBasicVer : TreeGeneratorBase
{
    private uint currentIteration = 0;
    
    [SerializeField]
    private List<BranchDataCPUVer> treeStarts = new List<BranchDataCPUVer>();


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
        currentIteration = 0;

        treeStarts = new List<BranchDataCPUVer>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override long GenerateTreeData()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        GenerateTreeDatas();

        stopwatch.Stop();

        if (debug)
        {
            UnityEngine.Debug.LogFormat("CPU (Recursive Function) Algorithm Complete: {0}ms", stopwatch.ElapsedMilliseconds.ToString());
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
    private void GenerateTreeDatas()
    {
        treeStarts = new List<BranchDataCPUVer>();

        for (int i = 0; i < numberOfGenerations; i++)
        {
            // Reset generation counters
            currentIteration = 0;

            // Start a new tree data generation
            CreateStartBranch();
        }
    }


    /// <summary>
    /// 
    /// </summary>
    private void CreateStartBranch()
    {
        // No point creating a start branch if the tree size is to be 0
        if (numberOfIterations == 0)
        {
            return;
        }

        // Creating the inital start "branch" of the tree
        // Start branch needs no transformation as it always point's upwards
        BranchDataCPUVer treeStart = new BranchDataCPUVer();

        // Starts the recursive function that iterates through the rest of the tree generation
        currentIteration++;
        if (currentIteration < numberOfIterations)
        {
            CreateNextSetOFSubBranches(new List<BranchDataCPUVer>() { treeStart });
        }

        // Finally stores the fully generated tree
        treeStarts.Add(treeStart);
    }
    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="previousBranches"></param>
    private void CreateNextSetOFSubBranches(List<BranchDataCPUVer> previousBranches)
    {
        List<BranchDataCPUVer> nextBranches = new List<BranchDataCPUVer>();

        // Create data for each needed child branch
        for (int i = 0; i < previousBranches.Count; i++)
        {
            for (int j = 0; j < numberOfBranchSplits; j++)
            {
                BranchDataCPUVer newBranch = new BranchDataCPUVer();
                nextBranches.Add(newBranch);

                // Sub branches of current branch angle out at equal but "opposite" angles
                Quaternion newRot = Quaternion.Euler(branchAngle, ((360.0f / numberOfBranchSplits) * j), 0);
                newBranch.branchRotation = newRot;

                newBranch.branchScale = branchSize;

                previousBranches[i].childBranches.Add(newBranch);
            }
        }
        
        // If tree hasn't reached it's max size continue to branch off of each new branch
        if (currentIteration < numberOfIterations)
        {
            currentIteration++;
            CreateNextSetOFSubBranches(nextBranches);
        }
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
        // Checking that a tree has actually been generated
        if (treeStarts.Count > 0)
        {
            for (int i = 0; i < treeStarts.Count; i++)
            {
                GameObject trunkModel = Instantiate(branchModelPrefab, this.transform);
                trunkModel.isStatic = true;
                //treeStart.spawnedPrefab = trunkModel;

                trunkModel.transform.position = new Vector3(i * 10, 0, 0);

                ContinueModelGeneration(treeStarts[i], trunkModel);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("ERROR: No Tree Data has been generated");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="previousData"></param>
    /// <param name="previousPrefab"></param>
    private void ContinueModelGeneration(BranchDataCPUVer previousData, GameObject previousPrefab)
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
            for (int i = 0; i < previousData.childBranches.Count; i++)
            {
                BranchDataCPUVer nextData = previousData.childBranches[i];

                // Creating model
                GameObject nextBranchPrefab = Instantiate(branchModelPrefab, this.transform);
                nextBranchPrefab.isStatic = true;
                //nextData.spawnedPrefab = nextBranchPrefab;

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
    #endregion
}