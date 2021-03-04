using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BranchDataBase
{
    public Quaternion branchRotation = Quaternion.identity;

    public float branchScale = 1;

    //public GameObject spawnedPrefab;
}

public class BranchDataCPUVer : BranchDataBase
{
    public List<BranchDataCPUVer> childBranches = new List<BranchDataCPUVer>();
}

public class BranchDataGPUVer : BranchDataBase
{
    public int numberOfChildBranches = -1;
    public int connectedChildBranches = 0;

    public int[] childBranchesIdx = new int[4];
}