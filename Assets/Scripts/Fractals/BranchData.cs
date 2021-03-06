using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine;

public class BranchDataBase
{
    public UnityEngine.Quaternion branchRotation = UnityEngine.Quaternion.identity;

    public float branchScale = 1;

    //public GameObject spawnedPrefab;
}

public class BranchDataCPUVer : BranchDataBase
{
    public List<BranchDataCPUVer> childBranches = new List<BranchDataCPUVer>();
}

public class BranchDataRefactoredVer : BranchDataBase
{
    public int numberOfChildBranches = -1;
    public int connectedChildBranches = 0;

    public int[] childBranchesIdx = new int[4];
}


[StructLayout(LayoutKind.Sequential)] // Required to make the struct blittable
public struct BranchDataGPUVer
{
    public UnityEngine.Quaternion rotation;
    public float scale;
    public int numberofChildBranches;
    public int numberofConnectedBranches;
    public Vector<int> childBranchesIdx;


    public BranchDataGPUVer(int maxConnectedBranches)
    {
        rotation = UnityEngine.Quaternion.identity;
        scale = 1.0f;
        numberofChildBranches = 0;
        numberofConnectedBranches = 0;
        childBranchesIdx = new Vector<int>();
    }
}