using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TreeGeneratorBasicVer : MonoBehaviour
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
    private BranchDataCPUVer treeStart = null;

    [Header("Model Representation Generation Details")]
    [SerializeField]
    private GameObject branchModelPrefab;

    // Start is called before the first frame update
    void Start()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        CreateStartBranch();

        StartModelGeneration();

        stopwatch.Stop();
        UnityEngine.Debug.LogFormat("CPU Algorithm Complete: {0}ms", stopwatch.ElapsedMilliseconds.ToString());
    }


    /*
    ============================================================================================================================================================================================================================================================================================================
    Traditional CPU Implementation of Fractal Tree Generation Algorithm
    ============================================================================================================================================================================================================================================================================================================
    */
    private void CreateStartBranch()
    {
        // No point creating a start branch if the tree size is to be 0
        if (numberOfIterations == 0)
        {
            return;
        }

        // Creating the inital start "branch" of the tree
        // Start branch needs no transformation as it always point's upwards
        treeStart = new BranchDataCPUVer();

        // Starts the recursive function that iterates through the rest of the tree generation
        currentIteration++;
        if (currentIteration < numberOfIterations)
        {
            CreateNextSetOFSubBranches(new List<BranchDataCPUVer>() { treeStart });
        }
    }

    private void CreateNextSetOFSubBranches(List<BranchDataCPUVer> previousBranches)
    {
        List<BranchDataCPUVer> nextBranches = new List<BranchDataCPUVer>();

        // Create data for each needed child branch
        for (int i = 0; i < previousBranches.Count; i++)
        {
            int numberOfBranchSplits = Random.Range((int)numberOfBranchSplitsMin, (int)numberOfBranchSplitsMax);

            for (int j = 0; j < numberOfBranchSplits; j++)
            {
                BranchDataCPUVer newBranch = new BranchDataCPUVer();
                nextBranches.Add(newBranch);

                // Sub branches of current branch angle out at equal but "opposite" angles
                float randRotation = Random.Range(branchAngleMin, branchAngleMax);
                Quaternion newRot = Quaternion.Euler(randRotation, ((360.0f / numberOfBranchSplits) * j), 0);
                newBranch.branchRotation = newRot;

                float randSize = Random.Range(branchSizeMin, branchSizeMax);
                newBranch.branchScale = branchSizeCurve.Evaluate((float)currentIteration / numberOfIterations) * randSize;

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


    /*
    ============================================================================================================================================================================================================================================================================================================
    Creating Visual Representation of each generated tree
    ============================================================================================================================================================================================================================================================================================================
    */
    private void StartModelGeneration()
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

                // Sizing Model
                nextBranchPrefab.transform.localScale = (Vector3.one * nextData.branchScale);

                // Continuing Tree Generation
                ContinueModelGeneration(nextData, nextBranchPrefab);
            }
        }
    }
}