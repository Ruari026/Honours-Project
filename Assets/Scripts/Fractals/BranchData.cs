﻿using System.Collections.Generic;
using System.Runtime.InteropServices;


public class BranchDataCPUVer
{
    public UnityEngine.Quaternion branchRotation = UnityEngine.Quaternion.identity;

    public float branchScale = 1;

    public List<BranchDataCPUVer> childBranches = new List<BranchDataCPUVer>();
}


public struct BranchDataRefactoredVer
{
    public UnityEngine.Quaternion branchRotation;
    public float branchScale;
    public int numberOfChildBranches;
    public int connectedChildBranches;
    public int childBranchesStartIdx;
}


[StructLayout(LayoutKind.Sequential)] // Required to make the struct blittable
public struct BranchDataGPUVer
{
    public UnityEngine.Vector3 rotation;
    public float scale;
    public int numberofChildBranches;
    public int numberofConnectedBranches;
    public int childBranchesStartIdx;
    public int padding;


    public BranchDataGPUVer(int maxConnectedBranches)
    {
        rotation = UnityEngine.Vector3.zero;
        scale = 1.0f;
        numberofChildBranches = 0;
        numberofConnectedBranches = 0;
        childBranchesStartIdx = -1;
        padding = -1;
    }
}