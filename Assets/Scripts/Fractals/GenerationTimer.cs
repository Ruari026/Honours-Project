using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationTimer : MonoBehaviour
{
    [SerializeField]
    private TreeGeneratorBase targetTreeGenerator = null;

    [SerializeField]
    [Min(1)]
    private int numberOfTests = 1;

    [SerializeField]
    private bool generateTreeModels = false;

    [Header("Test Output")]
    [SerializeField]
    private List<long> dispatchTimes = new List<long>();
    [SerializeField]
    private string formattedTimes = "";
    [SerializeField]
    private double averageTime = 0.0f;

    [Header("Debugging")]
    [SerializeField]
    private bool enableDebugMessages = false;

    // Start is called before the first frame update
    void Start()
    {
        // Running generation Test
        for (int i = 0; i < numberOfTests; i++)
        {
            targetTreeGenerator.SetDebug(enableDebugMessages);
            targetTreeGenerator.ResetData();

            long time = targetTreeGenerator.GenerateTreeData();
            dispatchTimes.Add(time);

            if (generateTreeModels)
            {
                targetTreeGenerator.GenerateTreeModels();
            }
        }

        // Formatting test times for easy test calculator input
        for (int i = 0; i < numberOfTests; i++)
        {
            formattedTimes += dispatchTimes[i].ToString();

            if (i < numberOfTests - 1)
            {
                formattedTimes += ", ";
            }
        }

        // Getting average time
        long totalTime = 0;
        foreach (long l in dispatchTimes)
        {
            totalTime += l;
        }
        averageTime = (totalTime / dispatchTimes.Count);
    }
}