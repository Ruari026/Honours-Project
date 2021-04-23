using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TreeGeneratorBase : MonoBehaviour
{
    [Header("Tree Data Generation General Details")]
    [Min(1)]
    [SerializeField]
    protected uint numberOfGenerations = 1;

    [Range(0, 90)]
    [SerializeField]
    protected float branchAngle = 0;

    [SerializeField]
    protected uint numberOfBranchSplits = 2;

    [SerializeField]
    protected float branchSize = 1;

    [SerializeField]
    protected uint numberOfIterations = 1;

    [Header("Model Representation Generation Details")]
    [SerializeField]
    protected GameObject branchModelPrefab = null;

    [Header("Debugging")]
    [SerializeField]
    protected bool debug = false;

    /// <summary>
    /// 
    /// </summary>
    public abstract void ResetData();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="debug"></param>
    /// <returns></returns>
    public abstract long GenerateTreeData();
    
    /// <summary>
    /// 
    /// </summary>
    public abstract void GenerateTreeModels();


    /// <summary>
    /// 
    /// </summary>
    /// <param name="debug"></param>
    public void SetDebug(bool debug)
    {
        this.debug = debug;
    }
}
