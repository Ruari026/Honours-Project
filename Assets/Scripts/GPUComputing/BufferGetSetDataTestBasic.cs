using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class BufferGetSetDataTestBasic : MonoBehaviour
{
    [SerializeField]
    private ComputeShader theShader;

    private ComputeBuffer dataBuffer;
    private ComputeBuffer argsBuffer;

    [Header("Test Settings")]
    [SerializeField]
    private uint bufferCount = 100;
    [SerializeField]
    private uint numberOfTests = 1;
    private Dictionary<TestProgramStage, long> testTimes = new Dictionary<TestProgramStage, long>();

    [Header("Test Outputs - Setup Time")]
    [SerializeField]
    private string dataSetupTimes = "";
    private List<long> _dataSetupTimes = new List<long>();
    [SerializeField]
    private float averageSetupTime = 0.0f;

    [Header("Test Outputs - Set Data Times")]
    [SerializeField]
    private string setDataTimes = "";
    private List<long> _setDataTimes = new List<long>();
    [SerializeField]
    private float averageSetTime = 0.0f;

    [Header("Test Outputs - Shader Dispatch Times")]
    [SerializeField]
    private string shaderDispatchTimes = "";
    private List<long> _shaderDispatchTimes = new List<long>();
    [SerializeField]
    private float averageDispatchTime = 0.0f;

    [Header("Test Outputs - Get Data Times")]
    [SerializeField]
    private string getDataTimes = "";
    private List<long> _getDataTimes = new List<long>();
    [SerializeField]
    private float averageGetTime = 0.0f;


    // Start is called before the first frame update
    void Start()
    {
        // Running individual tests
        StartCoroutine(RunTest(0));
    }

    private IEnumerator RunTest(int testNumber)
    {
        {
            testTimes = new Dictionary<TestProgramStage, long>();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            // Step 1 - Setting up data to set into buffers
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            BranchDataGPUVer[] inputTestDatas = new BranchDataGPUVer[bufferCount];
            // Buffer creation time
            int stride = Marshal.SizeOf(new BranchDataGPUVer());
            dataBuffer = new ComputeBuffer((int)bufferCount, stride);
            testTimes.Add(TestProgramStage.EXECUTIONPOINT_SETUP, stopwatch.ElapsedMilliseconds);


            // Step 2 - Buffer Setup + setting data + binding buffer
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // Buffer set Data time
            dataBuffer.SetData(inputTestDatas);
            // Need to know the count of the buffer and the stride (size in bytes) of each element in the buffer
            int kernel = theShader.FindKernel("CSMain"); 
            theShader.SetBuffer(kernel, nameof(dataBuffer), dataBuffer);
            testTimes.Add(TestProgramStage.EXECUTIONPOINT_SETDATA, stopwatch.ElapsedMilliseconds);


            // Step 3 - Dispatching Shader
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // Compute shader dispatch time
            theShader.Dispatch(kernel, 1, 1, 1);
            //yield return new WaitForSeconds(1.0f);
            yield return null;
            testTimes.Add(TestProgramStage.EXECUTIONPOINT_DISPATCH, stopwatch.ElapsedMilliseconds);


            // Step 4 - Getting Buffer Data
            // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // Buffer get data time
            BranchDataGPUVer[] outputTestDatas = new BranchDataGPUVer[bufferCount];
            dataBuffer.GetData(outputTestDatas);
            stopwatch.Stop();
            testTimes.Add(TestProgramStage.EXECUTIONPOINT_GETDATA, stopwatch.ElapsedMilliseconds);

            // Storing results
            long setupTime = (testTimes[TestProgramStage.EXECUTIONPOINT_SETUP]);
            long setDataTime = (testTimes[TestProgramStage.EXECUTIONPOINT_SETDATA] - testTimes[TestProgramStage.EXECUTIONPOINT_SETUP]);
            long dispatchTime = (testTimes[TestProgramStage.EXECUTIONPOINT_DISPATCH] - testTimes[TestProgramStage.EXECUTIONPOINT_SETDATA]);
            long getDataTime = (testTimes[TestProgramStage.EXECUTIONPOINT_GETDATA] - testTimes[TestProgramStage.EXECUTIONPOINT_DISPATCH]);


            dataSetupTimes += setupTime.ToString();
            setDataTimes += setDataTime.ToString();
            shaderDispatchTimes += dispatchTime.ToString();
            getDataTimes += getDataTime.ToString();
            if (testNumber != (numberOfTests - 1))
            {
                dataSetupTimes += ",";
                setDataTimes += ",";
                shaderDispatchTimes += ",";
                getDataTimes += ",";
            }


            _dataSetupTimes.Add(setupTime);
            _setDataTimes.Add(setDataTime);
            _shaderDispatchTimes.Add(dispatchTime);
            _getDataTimes.Add(getDataTime);


            // Cleaning up after test
            dataBuffer.Release();
        }

        testNumber++;
        if (testNumber < numberOfTests)
        {
            StartCoroutine(RunTest(testNumber));
        }
        else
        {
            CalculateAverages();
        }
    }

    private void CalculateAverages()
    {
        // Calculating average time for each stage
        float totalSetupTime = 0, totalSetDataTime = 0, totalDispatchTime = 0, totalGetDataTime = 0;
        for (int i = 0; i < numberOfTests; i++)
        {
            totalSetupTime += _dataSetupTimes[i];
            totalSetDataTime += _setDataTimes[i];
            totalDispatchTime += _shaderDispatchTimes[i];
            totalGetDataTime += _getDataTimes[i];
        }
        averageSetupTime = (totalSetupTime / numberOfTests);
        averageSetTime = (totalSetDataTime / numberOfTests);
        averageDispatchTime = (totalDispatchTime / numberOfTests);
        averageGetTime = (totalGetDataTime / numberOfTests);
    }
}

enum TestProgramStage
{ 
    EXECUTIONPOINT_SETUP = 0,
    EXECUTIONPOINT_SETDATA = 1,
    EXECUTIONPOINT_DISPATCH = 2,
    EXECUTIONPOINT_GETDATA = 3,
}